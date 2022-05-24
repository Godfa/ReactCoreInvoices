import { Invoice } from "Invoices";
import React from "react";
import { Grid } from "semantic-ui-react";
import InvoiceDetails from "../details/InvoiceDetails";
import InvoiceForm from "../form/InvoiceForm";
import InvoiceList from "./InvoiceList";

interface Props {
    invoices: Invoice[];
    selectedInvoice: Invoice | undefined;
    selectInvoice: (id: string) => void;
    cancelSelectInvoice: () => void;
    editMode: boolean;
    openForm: (id: string) => void;
    closeForm: () => void;
    createOrEdit: (invoice: Invoice) => void;
    deleteInvoice: (id: string) => void;
    submitting: boolean;

}

export default function InvoiceDashboard({ invoices, selectedInvoice, selectInvoice
    , cancelSelectInvoice, editMode, openForm,
     closeForm, createOrEdit, deleteInvoice, submitting }: Props) {
    return (
        <Grid>
            <Grid.Column width={'8'}>
                <InvoiceList invoices={invoices}
                    selectInvoice={selectInvoice}
                    deleteInvoice={deleteInvoice}
                    submitting={submitting} />
            </Grid.Column>
            <Grid.Column width='8'>
                {selectedInvoice && !editMode &&
                    <InvoiceDetails
                        invoice={selectedInvoice}
                        cancelSelectInvoice={cancelSelectInvoice}
                        openForm={openForm}
                    />}
                {editMode &&
                    <InvoiceForm
                        closeForm={closeForm}
                        invoice={selectedInvoice}
                        createOrEdit={createOrEdit}
                        submitting={submitting} />}
            </Grid.Column>
        </Grid>
    )
}