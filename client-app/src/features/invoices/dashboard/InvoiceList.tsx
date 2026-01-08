import { observer } from "mobx-react-lite";
import React, { SyntheticEvent, useState } from "react";
import { Button, Icon } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Link } from "react-router-dom";


export default observer(function InvoiceList() {
    const { invoiceStore } = useStore();
    const { deleteInvoice, Invoices, loading } = invoiceStore;

    const [target, setTarget] = useState('');

    function handleInvoiceDelete(e: SyntheticEvent<HTMLButtonElement>, id: string) {
        setTarget(e.currentTarget.name);
        deleteInvoice(id);
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
                            <h3 className="invoice-card-title">{invoice.title}</h3>
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
