declare module "Invoices" {

    export interface ExpenseItem {
        id: string;
        expenseCreditor: number;
        expenseType: number; // Enum value
        name: string;
    }

    export interface ExpenseTypeOption {
        key: number;
        value: string;
    }

    export interface Creditor {
        id: number;
        name: string;
    }

    export interface Invoice {
        id: string;
        lanNumber: number;
        description: string;
        title: string;
        image: string;
        amount: number;
        expenseItems: ExpenseItem[];
    }


}

