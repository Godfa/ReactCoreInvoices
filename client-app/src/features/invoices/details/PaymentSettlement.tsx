import React, { useMemo } from "react";
import { Invoice } from "../../../app/models/invoice";
import {
    calculateParticipantBalances,
    optimizePaymentTransactions,
    formatCurrency,
    getBalanceColor,
    ParticipantBalance,
    PaymentTransaction
} from "../../../app/utils/paymentCalculations";
import { Icon, Table, Segment, Header, Message } from "semantic-ui-react";
import "./PaymentSettlement.css";

interface Props {
    invoice: Invoice;
    compact?: boolean; // Jos true, näytetään vain maksutapahtumat
}

export default function PaymentSettlement({ invoice, compact = false }: Props) {
    const { balances, transactions, hasBalances } = useMemo(() => {
        const balances = calculateParticipantBalances(invoice);
        const transactions = optimizePaymentTransactions(balances);
        const hasBalances = balances.length > 0 && invoice.expenseItems.length > 0;

        return { balances, transactions, hasBalances };
    }, [invoice]);

    if (!hasBalances) {
        return (
            <Message info>
                <Icon name="info circle" />
                Ei maksutietoja näytettävissä. Lisää kuluja ja osallistujia nähdäksesi maksuerittelyt.
            </Message>
        );
    }

    if (transactions.length === 0) {
        return (
            <Message positive>
                <Icon name="check circle" />
                Kaikki saldot ovat tasapainossa! Ei avoimia maksuja.
            </Message>
        );
    }

    return (
        <div className="payment-settlement">
            {/* Maksutapahtumat (aina näytetään) */}
            <Segment className="glass-card">
                <Header as="h3">
                    <Icon name="exchange" />
                    Maksutapahtumat
                </Header>
                <p style={{ marginBottom: 'var(--spacing-md)', color: 'var(--text-secondary)' }}>
                    Optimoitu minimoiden maksutapahtumien määrä
                </p>

                <div className="payment-transactions">
                    {transactions.map((transaction, index) => (
                        <div key={index} className="payment-transaction-card">
                            <div className="transaction-from">
                                <Icon name="user" />
                                <strong>{transaction.fromUserName}</strong>
                            </div>
                            <div className="transaction-arrow">
                                <Icon name="long arrow alternate right" size="large" />
                                <div className="transaction-amount">
                                    {formatCurrency(transaction.amount)}
                                </div>
                            </div>
                            <div className="transaction-to">
                                <Icon name="user" />
                                <strong>{transaction.toUserName}</strong>
                            </div>
                        </div>
                    ))}
                </div>
            </Segment>

            {/* Yksityiskohtaiset saldot (piilotetaan compactissa näkymässä) */}
            {!compact && (
                <Segment className="glass-card" style={{ marginTop: 'var(--spacing-lg)' }}>
                    <Header as="h3">
                        <Icon name="calculator" />
                        Yksityiskohtaiset saldot
                    </Header>
                    <Table celled striped>
                        <Table.Header>
                            <Table.Row>
                                <Table.HeaderCell>Osallistuja</Table.HeaderCell>
                                <Table.HeaderCell textAlign="right">Maksettu</Table.HeaderCell>
                                <Table.HeaderCell textAlign="right">Osuus</Table.HeaderCell>
                                <Table.HeaderCell textAlign="right">Saldo</Table.HeaderCell>
                            </Table.Row>
                        </Table.Header>
                        <Table.Body>
                            {balances.map((balance) => (
                                <Table.Row key={balance.userId}>
                                    <Table.Cell>
                                        <strong>{balance.displayName}</strong>
                                    </Table.Cell>
                                    <Table.Cell textAlign="right">
                                        {formatCurrency(balance.totalPaid)}
                                    </Table.Cell>
                                    <Table.Cell textAlign="right">
                                        {formatCurrency(balance.totalOwed)}
                                    </Table.Cell>
                                    <Table.Cell
                                        textAlign="right"
                                        style={{
                                            color: `var(--${getBalanceColor(balance.netBalance)})`,
                                            fontWeight: 'bold'
                                        }}
                                    >
                                        {balance.netBalance > 0 ? '+' : ''}
                                        {formatCurrency(balance.netBalance)}
                                        {Math.abs(balance.netBalance) > 0.01 && (
                                            <div style={{ fontSize: '0.85em', fontWeight: 'normal', marginTop: '4px' }}>
                                                {balance.netBalance < 0
                                                    ? '(saa rahaa)'
                                                    : '(maksaa)'}
                                            </div>
                                        )}
                                    </Table.Cell>
                                </Table.Row>
                            ))}
                        </Table.Body>
                    </Table>

                    <Message info size="small">
                        <Icon name="info circle" />
                        <strong>Selite:</strong><br />
                        <strong>Maksettu</strong> = Kuinka paljon henkilö on maksanut kuluista (järjestäjänä)<br />
                        <strong>Osuus</strong> = Kuinka paljon henkilön pitää maksaa (osallistujana)<br />
                        <strong>Saldo</strong> = Erotus (negatiivinen = saa rahaa, positiivinen = maksaa)
                    </Message>
                </Segment>
            )}
        </div>
    );
}
