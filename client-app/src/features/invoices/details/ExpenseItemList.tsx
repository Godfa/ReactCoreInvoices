import { observer } from "mobx-react-lite";
import React, { useState } from "react";
import { Button, Segment, Table } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import ExpenseItemForm from "../form/ExpenseItemForm";
import { ExpenseItem } from "Invoices";

interface Props {
    invoiceId: string;
}

export default observer(function ExpenseItemList({ invoiceId }: Props) {
    const { invoiceStore } = useStore();
    const { selectedInvoice, deleteExpenseItem, loading, getExpenseTypeName, getCreditorName } = invoiceStore;
    const [addMode, setAddMode] = useState(false);
    const [editingItem, setEditingItem] = useState<ExpenseItem | null>(null);
    const [deletingId, setDeletingId] = useState<string>('');

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
                                {item.payers && item.payers.length > 0
                                    ? item.payers.map(p => p.creditor.name).join(', ')
                                    : 'No payers'}
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
