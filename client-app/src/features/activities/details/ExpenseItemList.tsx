import { observer } from "mobx-react-lite";
import React, { useState } from "react";
import { Button, Segment, Table, Icon } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import ExpenseItemForm from "../form/ExpenseItemForm";

interface Props {
    invoiceId: string;
}

export default observer(function ExpenseItemList({ invoiceId }: Props) {
    const { invoiceStore } = useStore();
    const { selectedInvoice, deleteExpenseItem, loading } = invoiceStore;
    const [addMode, setAddMode] = useState(false);

    if (!selectedInvoice) return null;

    return (
        <Segment>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                <h3>Expense Items</h3>
                <Button color='teal' content='Add Item' onClick={() => setAddMode(!addMode)} />
            </div>

            {addMode && (
                <ExpenseItemForm invoiceId={invoiceId} closeForm={() => setAddMode(false)} />
            )}

            <Table celled>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell>Name</Table.HeaderCell>
                        <Table.HeaderCell>Type</Table.HeaderCell>
                        <Table.HeaderCell>Creditor</Table.HeaderCell>
                        <Table.HeaderCell>Action</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>

                <Table.Body>
                    {selectedInvoice.expenseItems && selectedInvoice.expenseItems.map((item) => (
                        <Table.Row key={item.id}>
                            <Table.Cell>{item.name}</Table.Cell>
                            <Table.Cell>{item.expenseType}</Table.Cell>
                            <Table.Cell>{item.expenseCreditor}</Table.Cell>
                            <Table.Cell>
                                <Button
                                    loading={loading}
                                    onClick={() => deleteExpenseItem(invoiceId, item.id)}
                                    color='red'
                                    icon='trash'
                                    size='tiny'
                                />
                            </Table.Cell>
                        </Table.Row>
                    ))}
                </Table.Body>
            </Table>
        </Segment>
    )
})
