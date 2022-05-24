declare module "Invoices" {

    export interface ExpenseItem {
        id: string;
        expenseCreditor: number;
        expenseType: number;
        name: string;
    }

    export interface Invoice {
        id: string;
        lanNumber: string;
        description: string;
        title: string;
        image: string;
        amount: number;
        expenseItems: ExpenseItem[];
    }

}

