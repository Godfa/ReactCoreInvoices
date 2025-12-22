import { Invoice, ExpenseItem } from "Invoices";
import { makeAutoObservable, runInAction } from "mobx";
import agent from "../api/agent";
import { v4 as uuid } from 'uuid';
import { toast } from 'react-toastify';
export default class InvoiceStore {
    invoiceRegistry = new Map<string, Invoice>();
    selectedInvoice: Invoice | undefined = undefined;
    editMode = false;
    loading = false;
    loadingInitial = true;

    constructor() {
        makeAutoObservable(this)

    }

    get Invoices() {
        return Array.from(this.invoiceRegistry.values());
    }

    loadInvoices = async () => {
        // this.loadingInitial = true;
        try {
            const invoices = await agent.Invoices.list();
            invoices.forEach((invoice: Invoice) => {
                this.invoiceRegistry.set(invoice.id, invoice);

            })
            this.setLoadingInitial(false);
        } catch (error) {
            console.log(error);
            this.setLoadingInitial(false);
        }
    }

    setLoadingInitial = (state: boolean) => {
        this.loadingInitial = state;
    }

    selectInvoice = (id: string) => {
        this.selectedInvoice = this.invoiceRegistry.get(id);
    }

    cancelSelectedInvoice = () => {
        this.selectedInvoice = undefined;
    }

    openForm = (id?: string) => {
        id ? this.selectInvoice(id) : this.cancelSelectedInvoice();
        this.editMode = true;
    }

    closeForm = () => {
        this.editMode = false;
    }

    createInvoice = async (invoice: Invoice) => {
        this.loading = true;
        invoice.id = uuid();
        try {
            await agent.Invoices.create(invoice);
            runInAction(() => {
                this.invoiceRegistry.set(invoice.id, invoice);
                this.selectedInvoice = invoice;
                this.editMode = false;
                this.loading = false;
            })
            toast.success('Invoice created successfully');
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.loading = false;
            })
        }
    }

    updateInvoice = async (invoice: Invoice) => {
        this.loading = true;
        try {
            await agent.Invoices.update(invoice);
            runInAction(() => {
                this.invoiceRegistry.set(invoice.id, invoice);
                this.selectedInvoice = invoice;
                this.editMode = false;
                this.loading = false;
            })
            toast.success('Invoice updated successfully');
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.loading = false;
            })
        }
    }

    expenseTypeRegistry = new Map<number, string>();

    get ExpenseTypes() {
        return Array.from(this.expenseTypeRegistry.entries()).map(([key, value]) => ({ key, value }));
    }

    loadExpenseTypes = async () => {
        if (this.expenseTypeRegistry.size > 0) return;
        try {
            const types = await agent.ExpenseTypes.list();
            runInAction(() => {
                types.forEach(type => {
                    this.expenseTypeRegistry.set(type.key, type.value);
                })
            })
        } catch (error) {
            console.log(error);
        }
    }

    createExpenseItem = async (invoiceId: string, expenseItem: ExpenseItem) => {
        this.loading = true;
        try {
            await agent.ExpenseItems.createForInvoice(invoiceId, expenseItem);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice) {
                    if (!invoice.expenseItems) invoice.expenseItems = [];
                    invoice.expenseItems.push(expenseItem);
                    // Update registry
                    this.invoiceRegistry.set(invoiceId, invoice);
                    if (this.selectedInvoice?.id === invoiceId) {
                        this.selectedInvoice = invoice;
                    }
                }
                this.loading = false;
            })
            toast.success('Expense Item added');
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    deleteExpenseItem = async (invoiceId: string, itemId: string) => {
        this.loading = true;
        try {
            await agent.ExpenseItems.delete(itemId);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice && invoice.expenseItems) {
                    invoice.expenseItems = invoice.expenseItems.filter(i => i.id !== itemId);
                    this.invoiceRegistry.set(invoiceId, invoice);
                    if (this.selectedInvoice?.id === invoiceId) {
                        this.selectedInvoice = invoice;
                    }
                }
                this.loading = false;
            })
            toast.success('Expense Item deleted');
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    deleteInvoice = async (id: string) => {
        this.loading = true;
        try {
            await agent.Invoices.delete(id);
            runInAction(() => {
                this.invoiceRegistry.delete(id);
                if (this.selectedInvoice?.id === id) this.cancelSelectedInvoice();
                this.loading = false;
            })
            toast.success('Invoice deleted successfully');
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.loading = false;
            })
        }
    }
}
