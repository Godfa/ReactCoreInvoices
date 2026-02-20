import React, { useEffect, useMemo } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { Button, Header, Table, Icon, Label, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import LoadingComponent from "../../../app/layout/LoadingComponent";
import { ParticipantBalance, PaymentTransaction, Invoice, AppUser } from "../../../app/models/invoice";

// Helper functions for payment optimization
function calculateParticipantBalances(invoice: Invoice): ParticipantBalance[] {
    const balances = new Map<string, ParticipantBalance>();

    if (!invoice.participants || !invoice.expenseItems) {
        return [];
    }

    // Initialize all participants
    invoice.participants.forEach(participant => {
        balances.set(participant.appUserId, {
            userId: participant.appUserId,
            displayName: participant.appUser?.displayName || 'Unknown',
            totalPaid: 0,
            totalOwed: 0,
            netBalance: 0
        });
    });

    // Calculate paid and owed amounts
    invoice.expenseItems.forEach(expenseItem => {
        const totalAmount = expenseItem.amount;
        const payerCount = expenseItem.payers?.length || 0;

        if (payerCount === 0) return;

        const sharePerPerson = totalAmount / payerCount;

        // Add paid amount to organizer
        const organizerBalance = balances.get(expenseItem.organizerId);
        if (organizerBalance) {
            organizerBalance.totalPaid += totalAmount;
        }

        // Add owed amount to each payer
        expenseItem.payers?.forEach(payer => {
            const payerBalance = balances.get(payer.appUserId);
            if (payerBalance) {
                payerBalance.totalOwed += sharePerPerson;
            }
        });
    });

    // Calculate net balances
    // Negative = paid more than owed = others owe this person
    // Positive = owes more than paid = owes to others
    balances.forEach(balance => {
        balance.netBalance = balance.totalOwed - balance.totalPaid;
    });

    return Array.from(balances.values()).sort((a, b) => a.netBalance - b.netBalance);
}

function optimizePaymentTransactions(
    balances: ParticipantBalance[],
    invoice: Invoice
): PaymentTransaction[] {
    const transactions: PaymentTransaction[] = [];
    const epsilon = 0.01;

    // Separate debtors and creditors
    const debtors = balances
        .filter(b => b.netBalance > epsilon)
        .map(b => ({ userId: b.userId, displayName: b.displayName, amount: b.netBalance }));

    const creditors = balances
        .filter(b => b.netBalance < -epsilon)
        .map(b => ({ userId: b.userId, displayName: b.displayName, amount: -b.netBalance }))
        .sort((a, b) => b.amount - a.amount);

    if (creditors.length === 0 || debtors.length === 0) {
        return transactions;
    }

    // Main creditor is the "intermediary"
    const mainCreditor = creditors[0];
    const mainCreditorUser = invoice.participants?.find(p => p.appUserId === mainCreditor.userId)?.appUser;
    const otherCreditors = creditors.slice(1);

    // 1. All debtors pay to the main creditor
    debtors.forEach(debtor => {
        transactions.push({
            fromUserId: debtor.userId,
            fromUserName: debtor.displayName,
            toUserId: mainCreditor.userId,
            toUserName: mainCreditor.displayName,
            amount: debtor.amount,
            toUser: mainCreditorUser
        });
    });

    // 2. Main creditor pays to other creditors
    otherCreditors.forEach(creditor => {
        const creditorUser = invoice.participants?.find(p => p.appUserId === creditor.userId)?.appUser;
        transactions.push({
            fromUserId: mainCreditor.userId,
            fromUserName: mainCreditor.displayName,
            toUserId: creditor.userId,
            toUserName: creditor.displayName,
            amount: creditor.amount,
            toUser: creditorUser
        });
    });

    return transactions;
}

export default observer(function ParticipantInvoicePrintView() {
    const { invoiceStore } = useStore();
    const { selectedInvoice: invoice, loadInvoice, loadingInitial, getExpenseTypeName, loadExpenseTypes } = invoiceStore;
    const { id, participantId } = useParams<{ id: string; participantId: string }>();
    const navigate = useNavigate();

    useEffect(() => {
        if (id) {
            loadInvoice(id);
            loadExpenseTypes();
        }
    }, [id, loadInvoice, loadExpenseTypes]);

    const participantShare = useMemo(() => {
        if (!invoice || !participantId) return { expenses: [], total: 0, participantName: '' };

        const participant = invoice.participants.find(p => p.appUserId === participantId);
        const participantName = participant?.appUser.displayName || 'Tuntematon';

        const expenses = invoice.expenseItems
            .filter(item => item.payers && item.payers.some(p => p.appUserId === participantId))
            .map(item => {
                const payerCount = item.payers.length;
                const shareAmount = item.amount / payerCount;
                return {
                    ...item,
                    shareAmount,
                    payerCount
                };
            });

        const total = expenses.reduce((sum, item) => sum + item.shareAmount, 0);

        return { expenses, total, participantName };
    }, [invoice, participantId]);

    const participantPayments = useMemo(() => {
        if (!invoice || !participantId) return [];

        const balances = calculateParticipantBalances(invoice);
        const allTransactions = optimizePaymentTransactions(balances, invoice);

        // Filter to show only transactions where this participant is the payer
        return allTransactions.filter(t => t.fromUserId === participantId);
    }, [invoice, participantId]);

    if (loadingInitial || !invoice) return <LoadingComponent content="Ladataan laskua..." />;

    return (
        <div style={{ padding: '40px', maxWidth: '1000px', margin: '0 auto', background: 'white', color: 'black' }}>
            <style>
                {`
                    /* Force dark text for print view table */
                    #root .ui.table.print-view-table td,
                    #root .ui.table.print-view-table td *,
                    #root .ui.table.print-view-table td i {
                        color: #000 !important;
                    }

                    #root .ui.table.print-view-table th,
                    #root .ui.table.print-view-table th * {
                        color: #666 !important;
                    }

                    /* Fix participants labels */
                    #root .ui.label,
                    #root .ui.label * {
                        color: #000 !important;
                    }

                    @media print {
                        .no-print {
                            display: none !important;
                        }
                        body, html, #root, .print-container {
                            background-color: white !important;
                            color: black !important;
                            height: auto !important;
                            overflow: visible !important;
                        }
                        .print-container {
                            padding: 0;
                            margin: 0;
                            width: 100%;
                            box-shadow: none;
                        }
                        * {
                            color: black !important;
                            background-color: white !important;
                            -webkit-print-color-adjust: exact !important;
                            print-color-adjust: exact !important;
                        }
                        .ui.table {
                            border: 1px solid #ddd !important;
                        }
                        .ui.table th, .ui.table td {
                            border: 1px solid #ddd !important;
                            color: black !important;
                        }
                        @page {
                            margin: 2cm;
                        }
                    }
                `}
            </style>

            <div className="no-print" style={{ marginBottom: '20px', display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                <Button
                    icon
                    labelPosition='left'
                    onClick={() => navigate(`/invoices/${invoice.id}/print`)}
                >
                    <Icon name='arrow left' />
                    Takaisin päätulostussivulle
                </Button>
                <Button
                    icon
                    labelPosition='left'
                    onClick={() => navigate(`/invoices/${invoice.id}?tab=participants`)}
                >
                    <Icon name='arrow left' />
                    Takaisin osallistujalistaan
                </Button>
                <Button
                    icon
                    labelPosition='left'
                    onClick={() => navigate(`/invoices/${invoice.id}`)}
                >
                    <Icon name='arrow left' />
                    Takaisin laskulle
                </Button>
                <Button color='blue' icon labelPosition='left' onClick={() => window.print()}>
                    <Icon name='print' />
                    Tulosta
                </Button>
            </div>

            <div className="print-container">
                {/* Header Section */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '40px', borderBottom: '2px solid #eee', paddingBottom: '20px' }}>
                    <div>
                        <Header as='h1' style={{ fontSize: '2.5em', marginBottom: '10px' }}>
                            {invoice.title}
                        </Header>
                        <Header as='h3' sub style={{ marginTop: '0', color: 'gray' }}>
                            Osuus: {participantShare.participantName}
                        </Header>
                        <p style={{ marginTop: '5px', color: 'gray' }}>LAN #{invoice.lanNumber}</p>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                        <Header as='h2' color='blue' style={{ fontSize: '2em', margin: '0 0 5px 0' }}>
                            {participantShare.total.toFixed(2)} €
                        </Header>
                        <Label size='large'>
                            Yhteensä
                        </Label>
                    </div>
                </div>

                {/* Expenses */}
                <div style={{ marginBottom: '20px' }}>
                    <Header as='h3' dividing>Maksuosuudet</Header>
                    {participantShare.expenses.length > 0 ? (
                        <Table celled striped structured className='print-view-table'>
                            <Table.Header>
                                <Table.Row>
                                    <Table.HeaderCell width={5}>Nimi / Kuvaus</Table.HeaderCell>
                                    <Table.HeaderCell width={3}>Tyyppi</Table.HeaderCell>
                                    <Table.HeaderCell width={2}>Koko summa</Table.HeaderCell>
                                    <Table.HeaderCell width={2}>Jako</Table.HeaderCell>
                                    <Table.HeaderCell width={4}>Sinun osuus</Table.HeaderCell>
                                </Table.Row>
                            </Table.Header>
                            <Table.Body>
                                {participantShare.expenses.map(item => (
                                    <Table.Row key={item.id}>
                                        <Table.Cell>{item.name}</Table.Cell>
                                        <Table.Cell>{getExpenseTypeName(item.expenseType)}</Table.Cell>
                                        <Table.Cell>{item.amount.toFixed(2)} €</Table.Cell>
                                        <Table.Cell>1 / {item.payerCount}</Table.Cell>
                                        <Table.Cell style={{ fontWeight: 'bold' }}>{item.shareAmount.toFixed(2)} €</Table.Cell>
                                    </Table.Row>
                                ))}
                                <Table.Row>
                                    <Table.Cell colSpan='4' textAlign='right' style={{ fontWeight: 'bold' }}>Yhteensä:</Table.Cell>
                                    <Table.Cell style={{ fontWeight: 'bold', fontSize: '1.2em' }}>{participantShare.total.toFixed(2)} €</Table.Cell>
                                </Table.Row>
                            </Table.Body>
                        </Table>
                    ) : (
                        <p>Ei maksuosuuksia tässä laskussa.</p>
                    )}
                </div>

                {/* Payment Instructions */}
                {participantPayments.length > 0 && (
                    <div style={{ marginTop: '30px' }}>
                        <Header as='h3' dividing>Maksutiedot</Header>
                        {participantPayments.map((transaction, index) => (
                            <Segment key={index} style={{ padding: '15px', marginBottom: '15px', backgroundColor: '#f9fafb', border: '1px solid #d4d4d5' }}>
                                <div style={{ marginBottom: '10px' }}>
                                    <strong style={{ fontSize: '1.1em' }}>Maksettava (osuutesi miinus jo maksamasi): {transaction.amount.toFixed(2)} €</strong>
                                </div>
                                <div style={{ marginBottom: '10px' }}>
                                    <strong style={{ fontSize: '1.1em' }}>Maksa {transaction.toUserName}:lle</strong>
                                </div>
                                {transaction.toUser ? (
                                    <div style={{ marginTop: '10px' }}>
                                        {(() => {
                                            const hasBankAccount = !!transaction.toUser.bankAccount;
                                            const hasPhoneNumber = !!transaction.toUser.phoneNumber;
                                            const preferredMethod = transaction.toUser.preferredPaymentMethod;

                                            if (preferredMethod === "Pankki" && hasBankAccount) {
                                                return (
                                                    <>
                                                        <div style={{ marginBottom: '5px' }}>
                                                            <strong>Pankki:</strong> {transaction.toUser.bankAccount} <Label size='small' color='blue'>Ensisijainen</Label>
                                                        </div>
                                                        {hasPhoneNumber && (
                                                            <div>
                                                                <strong>MobilePay:</strong> {transaction.toUser.phoneNumber}
                                                            </div>
                                                        )}
                                                    </>
                                                );
                                            } else if (preferredMethod === "MobilePay" && hasPhoneNumber) {
                                                return (
                                                    <>
                                                        <div style={{ marginBottom: '5px' }}>
                                                            <strong>MobilePay:</strong> {transaction.toUser.phoneNumber} <Label size='small' color='blue'>Ensisijainen</Label>
                                                        </div>
                                                        {hasBankAccount && (
                                                            <div>
                                                                <strong>Pankki:</strong> {transaction.toUser.bankAccount}
                                                            </div>
                                                        )}
                                                    </>
                                                );
                                            } else {
                                                return (
                                                    <>
                                                        {hasBankAccount && (
                                                            <div style={{ marginBottom: '5px' }}>
                                                                <strong>Pankki:</strong> {transaction.toUser.bankAccount}
                                                            </div>
                                                        )}
                                                        {hasPhoneNumber && (
                                                            <div>
                                                                <strong>MobilePay:</strong> {transaction.toUser.phoneNumber}
                                                            </div>
                                                        )}
                                                        {!hasBankAccount && !hasPhoneNumber && (
                                                            <div style={{ fontStyle: 'italic' }}>
                                                                Ei maksutietoja saatavilla
                                                            </div>
                                                        )}
                                                    </>
                                                );
                                            }
                                        })()}
                                    </div>
                                ) : (
                                    <div style={{ marginTop: '10px', fontStyle: 'italic' }}>
                                        Ei maksutietoja saatavilla
                                    </div>
                                )}
                            </Segment>
                        ))}
                    </div>
                )}

                {participantPayments.length === 0 && participantShare.total > 0 && (
                    <div style={{ marginTop: '30px' }}>
                        <Header as='h3' dividing>Maksutiedot</Header>
                        <p style={{ fontStyle: 'italic' }}>Sinulla ei ole maksettavia maksuja.</p>
                    </div>
                )}
            </div>
        </div>
    );
});
