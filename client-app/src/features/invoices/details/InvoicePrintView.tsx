import React, { useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { Button, Header, Table, Icon, Label, Image } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import LoadingComponent from "../../../app/layout/LoadingComponent";

export default observer(function InvoicePrintView() {
    const { invoiceStore } = useStore();
    const { selectedInvoice: invoice, loadInvoice, loadingInitial, getExpenseTypeName, loadExpenseTypes } = invoiceStore;
    const { id } = useParams<{ id: string }>();

    useEffect(() => {
        if (id) {
            loadInvoice(id);
            loadExpenseTypes();
        }
    }, [id, loadInvoice, loadExpenseTypes]);

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

                    #root .ui.table.print-view-table .line-item-cell,
                    #root .ui.table.print-view-table .line-item-cell * {
                        color: #555 !important;
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
                    Takaisin
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
                            LAN #{invoice.lanNumber}
                        </Header>
                        {invoice.description && (
                            <p style={{ marginTop: '10px', maxWidth: '600px' }}>{invoice.description}</p>
                        )}
                    </div>
                    <div style={{ textAlign: 'right' }}>
                        <Header as='h2' color='blue' style={{ fontSize: '2em', margin: '0 0 5px 0' }}>
                            {invoice.amount?.toFixed(2)} €
                        </Header>
                        <Label size='large' color={invoice.status === 0 ? 'green' : invoice.status === 1 ? 'yellow' : 'grey'}>
                            {invoice.status === 0 ? 'Aktiivinen' : invoice.status === 1 ? 'Katselmoitavana' : 'Arkistoitu'}
                        </Label>
                    </div>
                </div>

                {/* Participants */}
                <div style={{ marginBottom: '40px' }}>
                    <Header as='h3' dividing>Osallistujat</Header>
                    <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                        {invoice.participants && invoice.participants.map(p => (
                            <Label key={p.appUserId} size='large'>
                                {p.appUser.displayName}
                            </Label>
                        ))}
                    </div>
                </div>

                {/* Expenses */}
                <div style={{ marginBottom: '20px' }}>
                    <Header as='h3' dividing>Kuluerät</Header>
                    <Table celled striped structured className='print-view-table'>
                        <Table.Header>
                            <Table.Row>
                                <Table.HeaderCell width={4}>Nimi / Kuvaus</Table.HeaderCell>
                                <Table.HeaderCell width={2}>Tyyppi</Table.HeaderCell>
                                <Table.HeaderCell width={3}>Maksaja (Velkoja)</Table.HeaderCell>
                                <Table.HeaderCell width={2}>Summa</Table.HeaderCell>
                                <Table.HeaderCell width={5}>Kenelle jaettu</Table.HeaderCell>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {invoice.expenseItems && invoice.expenseItems.map(item => (
                                <React.Fragment key={item.id}>
                                    <Table.Row style={{ fontWeight: 'bold' }}>
                                        <Table.Cell>{item.name}</Table.Cell>
                                        <Table.Cell>{getExpenseTypeName(item.expenseType)}</Table.Cell>
                                        <Table.Cell>{item.organizer?.displayName}</Table.Cell>
                                        <Table.Cell>{item.amount.toFixed(2)} €</Table.Cell>
                                        <Table.Cell>
                                            {item.payers && item.payers.map(p => p.appUser.displayName).join(', ')}
                                        </Table.Cell>
                                    </Table.Row>
                                    {item.lineItems && item.lineItems.length > 0 && item.lineItems.map(li => (
                                        <Table.Row key={li.id} className='line-item-row'>
                                            <Table.Cell className='line-item-cell' style={{ paddingLeft: '30px' }}>
                                                <Icon name='angle right' /> {li.name}
                                            </Table.Cell>
                                            <Table.Cell className='line-item-cell' colSpan='2' style={{ fontStyle: 'italic' }}>
                                                {li.quantity} x {li.unitPrice.toFixed(2)} €
                                            </Table.Cell>
                                            <Table.Cell className='line-item-cell' style={{ fontStyle: 'italic' }}>
                                                {(li.quantity * li.unitPrice).toFixed(2)} €
                                            </Table.Cell>
                                            <Table.Cell></Table.Cell>
                                        </Table.Row>
                                    ))}
                                </React.Fragment>
                            ))}
                        </Table.Body>
                    </Table>
                </div>
            </div>
        </div>
    );
});
