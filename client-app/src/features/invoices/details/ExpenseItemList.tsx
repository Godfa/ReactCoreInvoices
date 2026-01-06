import { observer } from "mobx-react-lite";
import React, { useState } from "react";
import { Button, Icon, Label, Segment, Table } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import ExpenseItemForm from "../form/ExpenseItemForm";
import ExpenseLineItemForm from "../form/ExpenseLineItemForm";
import { ExpenseItem, ExpenseLineItem } from "Invoices";
import { toast } from "react-toastify";

interface Props {
    invoiceId: string;
}

export default observer(function ExpenseItemList({ invoiceId }: Props) {
    const { invoiceStore } = useStore();
    const { selectedInvoice, deleteExpenseItem, deleteLineItem, loading, getExpenseTypeName, getCreditorName, addPayer, removePayer, Creditors } = invoiceStore;
    const [addMode, setAddMode] = useState(false);
    const [editingItem, setEditingItem] = useState<ExpenseItem | null>(null);
    const [deletingId, setDeletingId] = useState<string>('');
    const [payerMenuOpen, setPayerMenuOpen] = useState<string | null>(null);
    const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set());
    const [addingLineItemFor, setAddingLineItemFor] = useState<string | null>(null);
    const [editingLineItem, setEditingLineItem] = useState<{ expenseItemId: string, lineItem: ExpenseLineItem } | null>(null);

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

    const toggleExpanded = (itemId: string) => {
        const newExpanded = new Set(expandedItems);
        if (newExpanded.has(itemId)) {
            newExpanded.delete(itemId);
        } else {
            newExpanded.add(itemId);
        }
        setExpandedItems(newExpanded);
    };

    const handleDeleteLineItem = async (expenseItemId: string, lineItemId: string) => {
        await deleteLineItem(invoiceId, expenseItemId, lineItemId);
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
                        <Table.HeaderCell width={1}></Table.HeaderCell>
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
                        <React.Fragment key={item.id}>
                            <Table.Row>
                                <Table.Cell>
                                    <Button
                                        icon
                                        size='tiny'
                                        onClick={() => toggleExpanded(item.id)}
                                    >
                                        <Icon name={expandedItems.has(item.id) ? 'chevron down' : 'chevron right'} />
                                    </Button>
                                </Table.Cell>
                                <Table.Cell>{item.name}</Table.Cell>
                                <Table.Cell>{getExpenseTypeName(item.expenseType)}</Table.Cell>
                                <Table.Cell>{getCreditorName(item.expenseCreditor)}</Table.Cell>
                                <Table.Cell>{(item.lineItems?.reduce((sum, li) => sum + li.quantity * li.unitPrice, 0) ?? 0).toFixed(2)} €</Table.Cell>
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
                                    <div style={{ marginTop: '5px' }}>
                                        {selectedInvoice.participants && selectedInvoice.participants.length > 0 && (
                                            <>
                                                {selectedInvoice.participants.filter(p => !item.payers?.some(payer => payer.creditorId === p.creditorId)).length > 0 && (
                                                    <Button
                                                        size='tiny'
                                                        color='green'
                                                        content={`Add All (${selectedInvoice.participants.filter(p => !item.payers?.some(payer => payer.creditorId === p.creditorId)).length})`}
                                                        onClick={async () => {
                                                            const payersToAdd = selectedInvoice.participants!.filter(p => !item.payers?.some(payer => payer.creditorId === p.creditorId));
                                                            invoiceStore.loading = true;
                                                            try {
                                                                await Promise.all(
                                                                    payersToAdd.map(p => addPayer(invoiceId, item.id, p.creditorId, true))
                                                                );
                                                                toast.success(`${payersToAdd.length} payers added`);
                                                            } finally {
                                                                invoiceStore.loading = false;
                                                            }
                                                        }}
                                                        loading={loading}
                                                        disabled={loading}
                                                        style={{ marginRight: '5px', marginBottom: '5px' }}
                                                    />
                                                )}
                                                {item.payers && item.payers.length > 0 && (
                                                    <Button
                                                        size='tiny'
                                                        color='red'
                                                        content={`Remove All (${item.payers.length})`}
                                                        onClick={async () => {
                                                            const payersToRemove = [...item.payers!];
                                                            invoiceStore.loading = true;
                                                            try {
                                                                await Promise.all(
                                                                    payersToRemove.map(p => removePayer(invoiceId, item.id, p.creditorId, true))
                                                                );
                                                                toast.success(`${payersToRemove.length} payers removed`);
                                                            } finally {
                                                                invoiceStore.loading = false;
                                                            }
                                                        }}
                                                        loading={loading}
                                                        disabled={loading}
                                                        style={{ marginRight: '5px', marginBottom: '5px' }}
                                                    />
                                                )}
                                            </>
                                        )}
                                        {payerMenuOpen !== item.id && (
                                            <Button
                                                size='tiny'
                                                color='orange'
                                                content='Add Payer'
                                                onClick={() => setPayerMenuOpen(item.id)}
                                                disabled={loading}
                                                style={{ marginBottom: '5px' }}
                                            />
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

                            {expandedItems.has(item.id) && (
                                <Table.Row>
                                    <Table.Cell colSpan='7' style={{ padding: '10px 20px', backgroundColor: '#f9f9f9' }}>
                                        <div style={{ marginBottom: '10px' }}>
                                            <strong>Line Items:</strong>
                                            <Button
                                                size='tiny'
                                                color='teal'
                                                content='Add Line Item'
                                                floated='right'
                                                onClick={() => {
                                                    setAddingLineItemFor(item.id);
                                                    setEditingLineItem(null);
                                                }}
                                                disabled={loading}
                                            />
                                        </div>

                                        {(addingLineItemFor === item.id || editingLineItem?.expenseItemId === item.id) && (
                                            <ExpenseLineItemForm
                                                invoiceId={invoiceId}
                                                expenseItemId={item.id}
                                                closeForm={() => {
                                                    setAddingLineItemFor(null);
                                                    setEditingLineItem(null);
                                                }}
                                                lineItem={editingLineItem?.lineItem}
                                            />
                                        )}

                                        {item.lineItems && item.lineItems.length > 0 ? (
                                            <Table compact size='small'>
                                                <Table.Header>
                                                    <Table.Row>
                                                        <Table.HeaderCell>Name</Table.HeaderCell>
                                                        <Table.HeaderCell>Quantity</Table.HeaderCell>
                                                        <Table.HeaderCell>Unit Price</Table.HeaderCell>
                                                        <Table.HeaderCell>Total</Table.HeaderCell>
                                                        <Table.HeaderCell>Actions</Table.HeaderCell>
                                                    </Table.Row>
                                                </Table.Header>
                                                <Table.Body>
                                                    {item.lineItems.map(lineItem => (
                                                        <Table.Row key={lineItem.id}>
                                                            <Table.Cell>{lineItem.name}</Table.Cell>
                                                            <Table.Cell>{lineItem.quantity}</Table.Cell>
                                                            <Table.Cell>{lineItem.unitPrice.toFixed(2)} €</Table.Cell>
                                                            <Table.Cell>{(lineItem.quantity * lineItem.unitPrice).toFixed(2)} €</Table.Cell>
                                                            <Table.Cell>
                                                                <Button
                                                                    icon='edit'
                                                                    size='mini'
                                                                    color='blue'
                                                                    onClick={() => {
                                                                        setEditingLineItem({ expenseItemId: item.id, lineItem });
                                                                        setAddingLineItemFor(null);
                                                                    }}
                                                                    disabled={loading}
                                                                />
                                                                <Button
                                                                    icon='trash'
                                                                    size='mini'
                                                                    color='red'
                                                                    onClick={() => handleDeleteLineItem(item.id, lineItem.id)}
                                                                    loading={loading}
                                                                    disabled={loading}
                                                                />
                                                            </Table.Cell>
                                                        </Table.Row>
                                                    ))}
                                                </Table.Body>
                                            </Table>
                                        ) : (
                                            <p style={{ color: '#999', fontStyle: 'italic' }}>No line items added yet.</p>
                                        )}
                                    </Table.Cell>
                                </Table.Row>
                            )}
                        </React.Fragment>
                    ))}
                </Table.Body>
            </Table>
        </Segment>
    )
})
