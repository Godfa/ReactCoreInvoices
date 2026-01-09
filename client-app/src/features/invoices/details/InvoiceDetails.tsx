import React, { useEffect, useState } from "react";
import { Button, Icon, Dropdown } from "semantic-ui-react";
import LoadingComponent from "../../../app/layout/LoadingComponent";
import { useStore } from "../../../app/stores/store";
import ExpenseItemList from "./ExpenseItemList";
import ParticipantList from "./ParticipantList";
import { Link, useParams } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { InvoiceStatus } from "../../../app/models/invoice";

export default observer(function InvoiceDetails() {
    const { invoiceStore } = useStore();
    const { selectedInvoice: invoice, loadInvoice, loadingInitial, changeInvoiceStatus } = invoiceStore;
    const { id } = useParams<{ id: string }>();
    const [activeTab, setActiveTab] = useState<'expenses' | 'participants'>('expenses');

    const statusOptions = [
        { key: InvoiceStatus.Aktiivinen, text: 'Aktiivinen', value: InvoiceStatus.Aktiivinen },
        { key: InvoiceStatus.Katselmoitavana, text: 'Katselmoitavana', value: InvoiceStatus.Katselmoitavana },
        { key: InvoiceStatus.Arkistoitu, text: 'Arkistoitu', value: InvoiceStatus.Arkistoitu }
    ];

    function getStatusLabel(status: InvoiceStatus): string {
        switch(status) {
            case InvoiceStatus.Aktiivinen: return 'Aktiivinen';
            case InvoiceStatus.Katselmoitavana: return 'Katselmoitavana';
            case InvoiceStatus.Arkistoitu: return 'Arkistoitu';
            default: return 'Tuntematon';
        }
    }

    useEffect(() => {
        if (id) {
            loadInvoice(id);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id]);

    if (loadingInitial || !invoice) return <LoadingComponent content="Ladataan laskua..." />;

    return (
        <div className="animate-fade-in">
            {/* Hero Section */}
            <div className="invoice-hero">
                <img src={`/assets/lanImages/${invoice.image}.jpg`} alt={invoice.title} />
                <div className="invoice-hero-overlay">
                    <h1 className="invoice-hero-title">{invoice.title}</h1>
                    <div className="invoice-hero-meta">LAN #{invoice.lanNumber}</div>
                </div>
            </div>

            {/* Info Grid */}
            <div className="invoice-info-grid">
                <div className="invoice-info-item">
                    <div className="invoice-info-label">Summa</div>
                    <div className="invoice-info-value">{invoice.amount?.toFixed(2)}€</div>
                </div>
                <div className="invoice-info-item">
                    <div className="invoice-info-label">LAN-numero</div>
                    <div className="invoice-info-value">#{invoice.lanNumber}</div>
                </div>
                <div className="invoice-info-item">
                    <div className="invoice-info-label">Kuluja</div>
                    <div className="invoice-info-value">{invoice.expenseItems?.length || 0}</div>
                </div>
                <div className="invoice-info-item">
                    <div className="invoice-info-label">Osallistujia</div>
                    <div className="invoice-info-value">{invoice.participants?.length || 0}</div>
                </div>
                <div className="invoice-info-item">
                    <div className="invoice-info-label">Tila</div>
                    <div className="invoice-info-value">
                        <Dropdown
                            selection
                            value={invoice.status}
                            options={statusOptions}
                            onChange={(e, { value }) => {
                                if (value !== undefined && invoice.id) {
                                    changeInvoiceStatus(invoice.id, value as InvoiceStatus);
                                }
                            }}
                        />
                    </div>
                </div>
            </div>

            {/* Description */}
            {invoice.description && (
                <div className="glass-card" style={{ padding: 'var(--spacing-lg)', marginBottom: 'var(--spacing-xl)' }}>
                    <h4 style={{ marginBottom: 'var(--spacing-sm)' }}>Kuvaus</h4>
                    <p style={{ margin: 0 }}>{invoice.description}</p>
                </div>
            )}

            {/* Tabs */}
            <div className="tabs">
                <button
                    className={`tab-button ${activeTab === 'expenses' ? 'active' : ''}`}
                    onClick={() => setActiveTab('expenses')}
                >
                    <Icon name="shopping cart" /> Kulut
                </button>
                <button
                    className={`tab-button ${activeTab === 'participants' ? 'active' : ''}`}
                    onClick={() => setActiveTab('participants')}
                >
                    <Icon name="users" /> Osallistujat
                </button>
            </div>

            {/* Tab Content */}
            <div className="animate-fade-in">
                {activeTab === 'expenses' && <ExpenseItemList invoiceId={invoice.id} />}
                {activeTab === 'participants' && <ParticipantList invoiceId={invoice.id} />}
            </div>

            {/* Actions */}
            <div style={{ display: 'flex', gap: 'var(--spacing-md)', marginTop: 'var(--spacing-xl)', flexWrap: 'wrap' }}>
                <Button as={Link} to={`/manage/${invoice.id}`} className="btn-primary">
                    <Icon name="edit" /> Muokkaa laskua
                </Button>
                <Button as={Link} to='/invoices' className="btn-secondary">
                    <Icon name="arrow left" /> Takaisin
                </Button>
            </div>
        </div>
    )
})
