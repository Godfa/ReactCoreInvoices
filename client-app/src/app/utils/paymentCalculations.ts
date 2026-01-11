import { Invoice, ExpenseItem, InvoiceParticipant } from "../models/invoice";

export interface ParticipantBalance {
    userId: string;
    displayName: string;
    totalPaid: number;      // Kuinka paljon on maksanut (järjestäjänä)
    totalOwed: number;      // Kuinka paljon on velkaa (osuutena)
    netBalance: number;     // Negatiivinen = muut ovat velkaa tälle, Positiivinen = on velkaa muille
}

export interface PaymentTransaction {
    fromUserId: string;
    fromUserName: string;
    toUserId: string;
    toUserName: string;
    amount: number;
}

/**
 * Laskee jokaisen osallistujan nettosaldon
 */
export function calculateParticipantBalances(invoice: Invoice): ParticipantBalance[] {
    const balances: Map<string, ParticipantBalance> = new Map();

    // Tarkista että tarvittavat kentät ovat olemassa
    if (!invoice.participants || !invoice.expenseItems) {
        return [];
    }

    // Alusta kaikki osallistujat
    invoice.participants.forEach(participant => {
        balances.set(participant.appUserId, {
            userId: participant.appUserId,
            displayName: participant.appUser.displayName,
            totalPaid: 0,
            totalOwed: 0,
            netBalance: 0
        });
    });

    // Käy läpi jokainen kulu
    invoice.expenseItems.forEach(expenseItem => {
        const totalAmount = expenseItem.amount;
        const payerCount = expenseItem.payers.length;

        if (payerCount === 0) return;

        const sharePerPerson = totalAmount / payerCount;

        // Lisää maksettu summa järjestäjälle
        const organizerBalance = balances.get(expenseItem.organizerId);
        if (organizerBalance) {
            organizerBalance.totalPaid += totalAmount;
        }

        // Lisää velkaosuus jokaiselle maksajalle
        expenseItem.payers.forEach(payer => {
            const payerBalance = balances.get(payer.appUserId);
            if (payerBalance) {
                payerBalance.totalOwed += sharePerPerson;
            }
        });
    });

    // Laske nettosaldot
    balances.forEach(balance => {
        // Negatiivinen = on maksanut enemmän kuin velkaa = muut ovat velkaa tälle
        // Positiivinen = on velkaa enemmän kuin maksanut = on velkaa muille
        balance.netBalance = balance.totalOwed - balance.totalPaid;
    });

    return Array.from(balances.values())
        .sort((a, b) => a.netBalance - b.netBalance); // Pienin (negatiivisin) ensin
}

/**
 * Optimoi maksutapahtumat "välittäjä"-mallilla:
 * - Kaikki velalliseet maksavat suurimmalle saajalle
 * - Suurin saaja maksaa sitten muille saajille
 * Tämä takaa että jokainen pieni velallinen maksaa vain yhdelle henkilölle
 */
export function optimizePaymentTransactions(balances: ParticipantBalance[]): PaymentTransaction[] {
    const transactions: PaymentTransaction[] = [];
    const epsilon = 0.01;

    // Erota velalliseet ja saajat
    const debtors = balances
        .filter(b => b.netBalance > epsilon)
        .map(b => ({ userId: b.userId, displayName: b.displayName, amount: b.netBalance }));

    const creditors = balances
        .filter(b => b.netBalance < -epsilon)
        .map(b => ({ userId: b.userId, displayName: b.displayName, amount: -b.netBalance }))
        .sort((a, b) => b.amount - a.amount); // Suurimmasta pienimpään

    if (creditors.length === 0 || debtors.length === 0) {
        return transactions;
    }

    // Suurin saaja on "välittäjä"
    const mainCreditor = creditors[0];
    const otherCreditors = creditors.slice(1);

    // 1. Kaikki velalliseet maksavat suurimmalle saajalle
    debtors.forEach(debtor => {
        transactions.push({
            fromUserId: debtor.userId,
            fromUserName: debtor.displayName,
            toUserId: mainCreditor.userId,
            toUserName: mainCreditor.displayName,
            amount: debtor.amount
        });
    });

    // 2. Suurin saaja maksaa muille saajille
    otherCreditors.forEach(creditor => {
        transactions.push({
            fromUserId: mainCreditor.userId,
            fromUserName: mainCreditor.displayName,
            toUserId: creditor.userId,
            toUserName: creditor.displayName,
            amount: creditor.amount
        });
    });

    return transactions;
}

/**
 * Muotoilee summan euroiksi
 */
export function formatCurrency(amount: number): string {
    return new Intl.NumberFormat('fi-FI', {
        style: 'currency',
        currency: 'EUR'
    }).format(amount);
}

/**
 * Palauttaa värikoodin saldon perusteella
 */
export function getBalanceColor(netBalance: number): string {
    const epsilon = 0.01;
    if (Math.abs(netBalance) < epsilon) return 'gray';
    return netBalance < 0 ? 'green' : 'red'; // Negatiivinen = hyvä (muut velkaa), Positiivinen = velkaa
}
