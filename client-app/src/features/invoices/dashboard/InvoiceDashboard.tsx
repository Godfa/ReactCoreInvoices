import { observer } from "mobx-react-lite";
import React, { useEffect } from "react";
import { Grid } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import InvoiceList from "./InvoiceList";
import LoadingComponent from "../../../app/layout/LoadingComponent";


export default observer(function InvoiceDashboard() {

    const { invoiceStore } = useStore();

    useEffect(() => {
        invoiceStore.loadInvoices();
    }, [invoiceStore])

    if (invoiceStore.loadingInitial) return <LoadingComponent content='Loading invoices' />

    return (
        <Grid>
            <Grid.Column width={'16'}>
                <InvoiceList />
            </Grid.Column>
        </Grid>
    )

})