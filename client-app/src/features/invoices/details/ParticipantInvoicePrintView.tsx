import React, { useEffect, useMemo } from "react";
import { useParams, Link } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { Button, Header, Table, Icon, Label } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import LoadingComponent from "../../../app/layout/LoadingComponent";

export default observer(function ParticipantInvoicePrintView() {
    const { invoiceStore } = useStore();
    const { selectedInvoice: invoice, loadInvoice, loadingInitial, getExpenseTypeName, loadExpenseTypes } = invoiceStore;
    const { id, participantId } = useParams<{ id: string; participantId: string }>();

    useEffect(() => {
        if (id) {
            loadInvoice(id);
            loadExpenseTypes();
        }
    }, [id, loadInvoice, loadExpenseTypes]);

    const participantShare = useMemo(() => {
        if (!invoice || !participantId) return { expenses: [], total: 0, participantName: '' };

        const participant = invoice.participants.find(p => p.appUserId === participantId);
        const participantName = participant?.appUser.displayName || 'Tuntematon';

        const expenses = invoice.expenseItems
            .filter(item => item.payers && item.payers.some(p => p.appUserId === participantId))
            .map(item => {
                const payerCount = item.payers.length;
                const shareAmount = item.amount / payerCount;
                return {
                    ...item,
                    shareAmount,
                    payerCount
                };
            });

        const total = expenses.reduce((sum, item) => sum + item.shareAmount, 0);

        return { expenses, total, participantName };
    }, [invoice, participantId]);

    if (loadingInitial || !invoice) return <LoadingComponent content="Ladataan laskua..." />;

    return (
        <div style={{ padding: '40px', maxWidth: '1000px', margin: '0 auto', background: 'white', color: 'black' }}>
            <style>
                {`
                    /* Force dark text for print view table */
                    #root .ui.table.print-view-table td,
                    #root .ui.table.print-view-table td *,
                    #root .ui.table.print-view-table td i {
                        color: #000 !important;
                    }

                    #root .ui.table.print-view-table th,
                    #root .ui.table.print-view-table th * {
                        color: #666 !important;
                    }

                    /* Fix participants labels */
                    #root .ui.label,
                    #root .ui.label * {
                        color: #000 !important;
                    }

                    @media print {
                        .no-print {
                            display: none !important;
                        }
                        body, html, #root, .print-container {
                            background-color: white !important;
                            color: black !important;
                            height: auto !important;
                            overflow: visible !important;
                        }
                        .print-container {
                            padding: 0;
                            margin: 0;
                            width: 100%;
                            box-shadow: none;
                        }
                        * {
                            color: black !important;
                            background-color: white !important;
                            -webkit-print-color-adjust: exact !important;
                            print-color-adjust: exact !important;
                        }
                        .ui.table {
                            border: 1px solid #ddd !important;
                        }
                        .ui.table th, .ui.table td {
                            border: 1px solid #ddd !important;
                            color: black !important;
                        }
                        @page {
                            margin: 2cm;
                        }
                    }
                `}
            </style>

            <div className="no-print" style={{ marginBottom: '20px', display: 'flex', gap: '10px' }}>
                <Button as={Link} to={`/invoices/${invoice.id}`} icon labelPosition='left'>
                    <Icon name='arrow left' />
                    Takaisin osallistujalistaan
                </Button>
                <Button color='blue' icon labelPosition='left' onClick={() => window.print()}>
                    <Icon name='print' />
                    Tulosta
                </Button>
            </div>

            <div className="print-container">
                {/* Header Section */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '40px', borderBottom: '2px solid #eee', paddingBottom: '20px' }}>
                    <div>
                        <Header as='h1' style={{ fontSize: '2.5em', marginBottom: '10px' }}>
                            {invoice.title}
                        </Header>
                        <Header as='h3' sub style={{ marginTop: '0', color: 'gray' }}>
                            Osuus: {participantShare.participantName}
                        </Header>
                        <p style={{ marginTop: '5px', color: 'gray' }}>LAN #{invoice.lanNumber}</p>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                        <Header as='h2' color='blue' style={{ fontSize: '2em', margin: '0 0 5px 0' }}>
                            {participantShare.total.toFixed(2)} €
                        </Header>
                        <Label size='large'>
                            Yhteensä
                        </Label>
                    </div>
                </div>

                {/* Expenses */}
                <div style={{ marginBottom: '20px' }}>
                    <Header as='h3' dividing>Maksuosuudet</Header>
                    {participantShare.expenses.length > 0 ? (
                        <Table celled striped structured className='print-view-table'>
                            <Table.Header>
                                <Table.Row>
                                    <Table.HeaderCell width={5}>Nimi / Kuvaus</Table.HeaderCell>
                                    <Table.HeaderCell width={3}>Tyyppi</Table.HeaderCell>
                                    <Table.HeaderCell width={2}>Koko summa</Table.HeaderCell>
                                    <Table.HeaderCell width={2}>Jako</Table.HeaderCell>
                                    <Table.HeaderCell width={4}>Sinun osuus</Table.HeaderCell>
                                </Table.Row>
                            </Table.Header>
                            <Table.Body>
                                {participantShare.expenses.map(item => (
                                    <Table.Row key={item.id}>
                                        <Table.Cell>{item.name}</Table.Cell>
                                        <Table.Cell>{getExpenseTypeName(item.expenseType)}</Table.Cell>
                                        <Table.Cell>{item.amount.toFixed(2)} €</Table.Cell>
                                        <Table.Cell>1 / {item.payerCount}</Table.Cell>
                                        <Table.Cell style={{ fontWeight: 'bold' }}>{item.shareAmount.toFixed(2)} €</Table.Cell>
                                    </Table.Row>
                                ))}
                                <Table.Row>
                                    <Table.Cell colSpan='4' textAlign='right' style={{ fontWeight: 'bold' }}>Yhteensä:</Table.Cell>
                                    <Table.Cell style={{ fontWeight: 'bold', fontSize: '1.2em' }}>{participantShare.total.toFixed(2)} €</Table.Cell>
                                </Table.Row>
                            </Table.Body>
                        </Table>
                    ) : (
                        <p>Ei maksuosuuksia tässä laskussa.</p>
                    )}
                </div>
            </div>
        </div>
    );
});
