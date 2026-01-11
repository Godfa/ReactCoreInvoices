import { observer } from "mobx-react-lite";
import React, { SyntheticEvent, useState } from "react";
import { Button, Icon, Label } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Link } from "react-router-dom";
import { InvoiceStatus } from "../../../app/models/invoice";


export default observer(function InvoiceList() {
    const { invoiceStore } = useStore();
    const { deleteInvoice, Invoices, loading } = invoiceStore;

    const [target, setTarget] = useState('');

    function handleInvoiceDelete(e: SyntheticEvent<HTMLButtonElement>, id: string) {
        setTarget(e.currentTarget.name);
        deleteInvoice(id);
    }

    function getStatusLabel(status: InvoiceStatus): string {
        switch(status) {
            case InvoiceStatus.Aktiivinen: return 'Aktiivinen';
            case InvoiceStatus.Maksussa: return 'Maksussa';
            case InvoiceStatus.Arkistoitu: return 'Arkistoitu';
            default: return 'Tuntematon';
        }
    }

    function getStatusColor(status: InvoiceStatus): "red" | "orange" | "green" | "grey" {
        switch(status) {
            case InvoiceStatus.Aktiivinen: return 'green';
            case InvoiceStatus.Maksussa: return 'orange';
            case InvoiceStatus.Arkistoitu: return 'grey';
            default: return 'grey';
        }
    }

    if (Invoices.length === 0) {
        return (
            <div className="empty-state">
                <div className="empty-state-icon">
                    <Icon name="file alternate outline" />
                </div>
                <div className="empty-state-title">Ei laskuja</div>
                <div className="empty-state-description">
                    Luo ensimmäinen laskusi aloittaaksesi.
                </div>
                <Button
                    as={Link}
                    to="/createInvoice"
                    className="btn-primary"
                    style={{ marginTop: 'var(--spacing-lg)' }}
                >
                    <Icon name="plus" /> Luo lasku
                </Button>
            </div>
        );
    }

    return (
        <div className="invoice-grid">
            {Invoices.map(invoice => (
                <div key={invoice.id} className="invoice-card">
                    <div className="invoice-card-header">
                        <div>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '8px' }}>
                                <h3 className="invoice-card-title" style={{ margin: 0 }}>{invoice.title}</h3>
                                <Label color={getStatusColor(invoice.status)} size="small">
                                    {getStatusLabel(invoice.status)}
                                </Label>
                            </div>
                            <div className="invoice-card-meta">
                                LAN #{invoice.lanNumber}
                            </div>
                        </div>
                        <div className="invoice-card-amount">
                            {invoice.amount?.toFixed(2)}€
                        </div>
                    </div>

                    {invoice.description && (
                        <div className="invoice-card-description">
                            {invoice.description}
                        </div>
                    )}

                    <div className="invoice-card-actions">
                        <Button
                            as={Link}
                            to={`/invoices/${invoice.id}`}
                            className="btn-primary"
                            size="small"
                        >
                            <Icon name="eye" /> Näytä
                        </Button>
                        <Button
                            name={invoice.id}
                            loading={loading && target === invoice.id}
                            onClick={(e) => handleInvoiceDelete(e, invoice.id)}
                            className="btn-danger"
                            size="small"
                        >
                            <Icon name="trash" /> Poista
                        </Button>
                    </div>
                </div>
            ))}
        </div>
    )
})
