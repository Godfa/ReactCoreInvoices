import { Invoice, ExpenseItem, Creditor } from "Invoices";
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

    // Creditor mapping
    creditorRegistry = new Map<number, string>();

    get Creditors() {
        return Array.from(this.creditorRegistry.entries()).map(([key, value]) => ({ key, value }));
    }

    loadCreditors = async () => {
        if (this.creditorRegistry.size > 0) return;
        try {
            const creditors = await agent.Creditors.list();
            runInAction(() => {
                creditors.forEach(creditor => {
                    this.creditorRegistry.set(creditor.id, creditor.name);
                })
            })
        } catch (error) {
            console.log(error);
        }
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
            const createdInvoice = await agent.Invoices.create(invoice);
            runInAction(() => {
                this.invoiceRegistry.set(createdInvoice.id, createdInvoice);
                this.selectedInvoice = createdInvoice;
                this.editMode = false;
                this.loading = false;
            })
            toast.success('Invoice created successfully');

            // Automatically add usual suspects as participants
            try {
                await this.addUsualSuspects(createdInvoice.id);
            } catch (error) {
                console.error('Failed to add usual suspects:', error);
            }
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

    updateExpenseItem = async (invoiceId: string, expenseItem: ExpenseItem) => {
        this.loading = true;
        try {
            await agent.ExpenseItems.update(expenseItem);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice && invoice.expenseItems) {
                    const index = invoice.expenseItems.findIndex(i => i.id === expenseItem.id);
                    if (index !== -1) {
                        invoice.expenseItems[index] = expenseItem;
                        this.invoiceRegistry.set(invoiceId, invoice);
                        if (this.selectedInvoice?.id === invoiceId) {
                            this.selectedInvoice = invoice;
                        }
                    }
                }
                this.loading = false;
            })
            toast.success('Expense Item updated');
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    getExpenseTypeName = (typeId: number): string => {
        return this.expenseTypeRegistry.get(typeId) || typeId.toString();
    }

    getCreditorName = (creditorId: number): string => {
        return this.creditorRegistry.get(creditorId) || creditorId.toString();
    }

    addParticipant = async (invoiceId: string, creditorId: number, silent: boolean = false) => {
        this.loading = true;
        try {
            await agent.Invoices.addParticipant(invoiceId, creditorId);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice) {
                    if (!invoice.participants) invoice.participants = [];
                    const creditor = { id: creditorId, name: this.creditorRegistry.get(creditorId) || '' };
                    invoice.participants.push({
                        invoiceId: invoiceId,
                        creditorId: creditorId,
                        creditor: creditor
                    });
                    this.invoiceRegistry.set(invoiceId, invoice);
                    if (this.selectedInvoice?.id === invoiceId) {
                        this.selectedInvoice = invoice;
                    }
                }
                this.loading = false;
            });
            if (!silent) {
                toast.success('Participant added');
            }
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    removeParticipant = async (invoiceId: string, creditorId: number) => {
        this.loading = true;
        try {
            await agent.Invoices.removeParticipant(invoiceId, creditorId);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice && invoice.participants) {
                    invoice.participants = invoice.participants.filter(p => p.creditorId !== creditorId);
                    this.invoiceRegistry.set(invoiceId, invoice);
                    if (this.selectedInvoice?.id === invoiceId) {
                        this.selectedInvoice = invoice;
                    }
                }
                this.loading = false;
            });
            toast.success('Participant removed');
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    addPayer = async (invoiceId: string, expenseItemId: string, creditorId: number) => {
        this.loading = true;
        try {
            await agent.ExpenseItems.addPayer(expenseItemId, creditorId);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice && invoice.expenseItems) {
                    const expenseItem = invoice.expenseItems.find(ei => ei.id === expenseItemId);
                    if (expenseItem) {
                        if (!expenseItem.payers) expenseItem.payers = [];
                        const creditor = { id: creditorId, name: this.creditorRegistry.get(creditorId) || '' };
                        expenseItem.payers.push({
                            expenseItemId: expenseItemId,
                            creditorId: creditorId,
                            creditor: creditor
                        });
                        this.invoiceRegistry.set(invoiceId, invoice);
                        if (this.selectedInvoice?.id === invoiceId) {
                            this.selectedInvoice = invoice;
                        }
                    }
                }
                this.loading = false;
            });
            toast.success('Payer added');
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    removePayer = async (invoiceId: string, expenseItemId: string, creditorId: number) => {
        this.loading = true;
        try {
            await agent.ExpenseItems.removePayer(expenseItemId, creditorId);
            runInAction(() => {
                const invoice = this.invoiceRegistry.get(invoiceId);
                if (invoice && invoice.expenseItems) {
                    const expenseItem = invoice.expenseItems.find(ei => ei.id === expenseItemId);
                    if (expenseItem && expenseItem.payers) {
                        expenseItem.payers = expenseItem.payers.filter(p => p.creditorId !== creditorId);
                        this.invoiceRegistry.set(invoiceId, invoice);
                        if (this.selectedInvoice?.id === invoiceId) {
                            this.selectedInvoice = invoice;
                        }
                    }
                }
                this.loading = false;
            });
            toast.success('Payer removed');
        } catch (error) {
            console.log(error);
            runInAction(() => this.loading = false);
        }
    }

    addUsualSuspects = async (invoiceId: string) => {
        const usualSuspects = ['Epi', 'JHattu', 'Leivo', 'Timo', 'Jaapu', 'Urpi', 'Zeip'];
        const invoice = this.invoiceRegistry.get(invoiceId);
        const participantIds = invoice?.participants?.map(p => p.creditorId) || [];

        const suspectsToAdd = this.Creditors.filter(c =>
            usualSuspects.includes(c.value) && !participantIds.includes(c.key)
        );

        for (const creditor of suspectsToAdd) {
            await this.addParticipant(invoiceId, creditor.key, true);
        }

        if (suspectsToAdd.length > 0) {
            toast.success(`${suspectsToAdd.length} usual suspects added as participants`);
        }
    }
}
