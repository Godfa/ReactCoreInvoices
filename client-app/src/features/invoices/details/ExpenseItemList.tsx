import { observer } from "mobx-react-lite";
import React, { useState } from "react";
import { Button, Label, Segment, Table } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import ExpenseItemForm from "../form/ExpenseItemForm";
import { ExpenseItem } from "Invoices";

interface Props {
    invoiceId: string;
}

export default observer(function ExpenseItemList({ invoiceId }: Props) {
    const { invoiceStore } = useStore();
    const { selectedInvoice, deleteExpenseItem, loading, getExpenseTypeName, getCreditorName, addPayer, removePayer, Creditors } = invoiceStore;
    const [addMode, setAddMode] = useState(false);
    const [editingItem, setEditingItem] = useState<ExpenseItem | null>(null);
    const [deletingId, setDeletingId] = useState<string>('');
    const [payerMenuOpen, setPayerMenuOpen] = useState<string | null>(null);

    if (!selectedInvoice) return null;

    const handleEdit = (item: ExpenseItem) => {
        setEditingItem(item);
        setAddMode(false);
    };

    const handleCloseForm = () => {
        setAddMode(false);
        setEditingItem(null);
    };

    const handleDelete = async (itemId: string) => {
        setDeletingId(itemId);
        await deleteExpenseItem(invoiceId, itemId);
        setDeletingId('');
    };

    return (
        <Segment>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                <h3>Expense Items</h3>
                <Button
                    color='teal'
                    content='Add Item'
                    onClick={() => {
                        setAddMode(!addMode);
                        setEditingItem(null);
                    }}
                    loading={loading}
                    disabled={loading}
                />
            </div>

            {(addMode || editingItem) && (
                <ExpenseItemForm
                    invoiceId={invoiceId}
                    closeForm={handleCloseForm}
                    expenseItem={editingItem || undefined}
                />
            )}

            <Table celled>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell>Name</Table.HeaderCell>
                        <Table.HeaderCell>Type</Table.HeaderCell>
                        <Table.HeaderCell>Creditor</Table.HeaderCell>
                        <Table.HeaderCell>Amount</Table.HeaderCell>
                        <Table.HeaderCell>Payers</Table.HeaderCell>
                        <Table.HeaderCell>Actions</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>

                <Table.Body>
                    {selectedInvoice.expenseItems && selectedInvoice.expenseItems.map((item) => (
                        <Table.Row key={item.id}>
                            <Table.Cell>{item.name}</Table.Cell>
                            <Table.Cell>{getExpenseTypeName(item.expenseType)}</Table.Cell>
                            <Table.Cell>{getCreditorName(item.expenseCreditor)}</Table.Cell>
                            <Table.Cell>{item.amount.toFixed(2)} €</Table.Cell>
                            <Table.Cell>
                                <div style={{ marginBottom: '5px' }}>
                                    {item.payers && item.payers.length > 0 ? (
                                        item.payers.map(p => (
                                            <Label key={p.creditorId} style={{ marginRight: '5px', marginBottom: '5px' }}>
                                                {p.creditor.name}
                                                <Label.Detail>
                                                    <Button
                                                        size='mini'
                                                        icon='delete'
                                                        onClick={() => removePayer(invoiceId, item.id, p.creditorId)}
                                                        loading={loading}
                                                        disabled={loading}
                                                        style={{ marginLeft: '5px', padding: '3px' }}
                                                    />
                                                </Label.Detail>
                                            </Label>
                                        ))
                                    ) : (
                                        <span style={{ color: '#999' }}>No payers</span>
                                    )}
                                </div>
                                {payerMenuOpen === item.id && (
                                    <div style={{ marginTop: '5px' }}>
                                        {Creditors
                                            .filter(c => !item.payers?.some(p => p.creditorId === c.key))
                                            .map(creditor => (
                                                <Button
                                                    key={creditor.key}
                                                    size='tiny'
                                                    color='orange'
                                                    basic
                                                    content={creditor.value}
                                                    onClick={() => {
                                                        addPayer(invoiceId, item.id, creditor.key);
                                                        setPayerMenuOpen(null);
                                                    }}
                                                    loading={loading}
                                                    disabled={loading}
                                                    style={{ marginRight: '5px', marginBottom: '5px' }}
                                                />
                                            ))}
                                        <Button
                                            size='tiny'
                                            content='Close'
                                            onClick={() => setPayerMenuOpen(null)}
                                            style={{ marginBottom: '5px' }}
                                        />
                                    </div>
                                )}
                                {payerMenuOpen !== item.id && (
                                    <Button
                                        size='tiny'
                                        color='orange'
                                        content='Add Payer'
                                        onClick={() => setPayerMenuOpen(item.id)}
                                        disabled={loading}
                                        style={{ marginTop: '5px' }}
                                    />
                                )}
                            </Table.Cell>
                            <Table.Cell>
                                <Button
                                    onClick={() => handleEdit(item)}
                                    color='blue'
                                    icon='edit'
                                    size='tiny'
                                    disabled={loading}
                                />
                                <Button
                                    loading={loading && deletingId === item.id}
                                    onClick={() => handleDelete(item.id)}
                                    color='red'
                                    icon='trash'
                                    size='tiny'
                                    disabled={loading && deletingId !== item.id}
                                />
                            </Table.Cell>
                        </Table.Row>
                    ))}
                </Table.Body>
            </Table>
        </Segment>
    )
})
