import { observer } from "mobx-react-lite";
import React from "react";
import { Grid } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import InvoiceDetails from "../details/InvoiceDetails";
import InvoiceForm from "../form/InvoiceForm";
import InvoiceList from "./InvoiceList";


export default observer(function InvoiceDashboard() {

        const {invoiceStore} = useStore();
        const {selectedInvoice, editMode} = invoiceStore;
    return (
        <Grid>
            <Grid.Column width={'8'}>
                <InvoiceList />
            </Grid.Column>
            <Grid.Column width='8'>
                {selectedInvoice && !editMode &&
                    <InvoiceDetails />}
                {editMode &&
                    <InvoiceForm />}
            </Grid.Column>
        </Grid>
    )

})