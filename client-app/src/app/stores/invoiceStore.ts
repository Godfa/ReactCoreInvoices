import { Invoice } from "Invoices";
import { makeAutoObservable, runInAction } from "mobx";
import agent from "../api/agent";
import {v4 as uuid} from 'uuid';
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
            invoices.forEach(invoice => {
                this.invoiceRegistry.set(invoice.id, invoice);

            })
            this.setLoadingInitial(false);          
        } catch (error) {
            console.log(error);
            this.setLoadingInitial(false);    
        }
    }

    setLoadingInitial = (state : boolean) => {
        this.loadingInitial = state;
    }
    
    selectInvoice = (id: string) => {
        this.selectedInvoice = this.invoiceRegistry.get(id);
    }

    cancelSelectedInvoice = () =>  {
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
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.loading = false;
            })
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
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.loading = false;
            })
        }
    }
}
