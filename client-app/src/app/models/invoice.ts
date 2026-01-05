declare module "Invoices" {

    export interface ExpenseLineItem {
        id: string;
        expenseItemId: string;
        name: string;
        quantity: number;
        unitPrice: number;
        total: number;
    }

    export interface ExpenseItemPayer {
        expenseItemId: string;
        creditorId: number;
        creditor: Creditor;
    }

    export interface ExpenseItem {
        id: string;
        expenseCreditor: number;
        expenseType: number; // Enum value
        name: string;
        amount: number;
        payers: ExpenseItemPayer[];
        lineItems: ExpenseLineItem[];
    }

    export interface ExpenseTypeOption {
        key: number;
        value: string;
    }

    export interface Creditor {
        id: number;
        name: string;
        email: string;
    }

    export interface InvoiceParticipant {
        invoiceId: string;
        creditorId: number;
        creditor: Creditor;
    }

    export interface Invoice {
        id: string;
        lanNumber: number;
        description: string;
        title: string;
        image: string;
        amount: number;
        expenseItems: ExpenseItem[];
        participants: InvoiceParticipant[];
    }


}

