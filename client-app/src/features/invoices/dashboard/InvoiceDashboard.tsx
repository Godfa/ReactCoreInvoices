import { observer } from "mobx-react-lite";
import React, { useEffect } from "react";
import { Icon } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import InvoiceList from "./InvoiceList";
import LoadingComponent from "../../../app/layout/LoadingComponent";


export default observer(function InvoiceDashboard() {

    const { invoiceStore } = useStore();

    useEffect(() => {
        invoiceStore.loadInvoices();
    }, [invoiceStore])

    if (invoiceStore.loadingInitial) return <LoadingComponent content='Ladataan laskuja...' />

    // Calculate stats
    const totalInvoices = invoiceStore.Invoices.length;
    const totalAmount = invoiceStore.Invoices.reduce((sum, inv) => sum + (inv.amount || 0), 0);

    return (
        <div className="animate-fade-in">
            <h1 style={{ marginBottom: 'var(--spacing-xl)' }}>Laskut</h1>

            {/* Stats Cards */}
            <div className="stats-grid">
                <div className="stat-card">
                    <div className="stat-icon purple">
                        <Icon name="file alternate outline" />
                    </div>
                    <div className="stat-content">
                        <div className="stat-label">Yhteensä laskuja</div>
                        <div className="stat-value">{totalInvoices}</div>
                    </div>
                </div>

                <div className="stat-card">
                    <div className="stat-icon blue">
                        <Icon name="euro sign" />
                    </div>
                    <div className="stat-content">
                        <div className="stat-label">Kokonaissumma</div>
                        <div className="stat-value">{totalAmount.toFixed(2)}€</div>
                    </div>
                </div>
            </div>

            {/* Invoice List */}
            <InvoiceList />
        </div>
    )

})
