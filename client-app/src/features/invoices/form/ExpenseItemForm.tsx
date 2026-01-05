import { observer } from "mobx-react-lite";
import React, { useEffect } from "react";
import { Button, Form, Header, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Formik, Form as FormikForm, Field, ErrorMessage } from "formik";
import * as Yup from 'yup';
import { v4 as uuid } from 'uuid';
import { ExpenseItem } from "Invoices";

interface Props {
    invoiceId: string;
    closeForm: () => void;
    expenseItem?: ExpenseItem;
}

interface FormValues {
    name: string;
    expenseType: number;
    expenseCreditor: number;
    id: string;
    payers: any[];
    lineItems: any[];
}

export default observer(function ExpenseItemForm({ invoiceId, closeForm, expenseItem }: Props) {
    const { invoiceStore } = useStore();
    const { createExpenseItem, updateExpenseItem, loading, loadExpenseTypes, loadCreditors, ExpenseTypes, Creditors } = invoiceStore;

    useEffect(() => {
        loadExpenseTypes();
        loadCreditors();
    }, [loadExpenseTypes, loadCreditors]);

    const validationSchema = Yup.object({
        name: Yup.string().required('The event name is required'),
        expenseType: Yup.number().required('Expense Type is required').notOneOf([-1], 'Type is required'),
        expenseCreditor: Yup.number().required('Creditor is required').notOneOf([-1], 'Creditor is required')
    })

    const initialValues: FormValues = expenseItem ? {
        name: expenseItem.name,
        expenseType: expenseItem.expenseType,
        expenseCreditor: expenseItem.expenseCreditor,
        id: expenseItem.id,
        payers: expenseItem.payers || [],
        lineItems: expenseItem.lineItems || []
    } : {
        name: '',
        expenseType: -1,
        expenseCreditor: -1,
        id: '',
        payers: [],
        lineItems: []
    }

    return (
        <Segment clearing>
            <Header content={expenseItem ? 'Edit Expense Item' : 'Add Expense Item'} sub color='teal' />
            <Formik
                initialValues={initialValues}
                onSubmit={(values) => {
                    const itemData = {
                        ...values,
                        expenseType: parseInt(values.expenseType.toString()),
                        expenseCreditor: parseInt(values.expenseCreditor.toString()),
                        id: expenseItem ? expenseItem.id : uuid()
                    };
                    const action = expenseItem
                        ? updateExpenseItem(invoiceId, itemData)
                        : createExpenseItem(invoiceId, itemData);
                    return action.then(() => closeForm());
                }}
                validationSchema={validationSchema}
            >
                {({ handleSubmit, isValid, isSubmitting, dirty }) => (
                    <FormikForm className='ui form' onSubmit={handleSubmit} autoComplete='off'>
                        <Form.Field>
                            <label>Name</label>
                            <Field placeholder='Name' name='name' />
                            <ErrorMessage name='name' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                        </Form.Field>
                        <Form.Field>
                            <label>Creditor</label>
                            <Field as="select" name="expenseCreditor">
                                <option value={-1}>Select Creditor</option>
                                {Creditors.map(creditor => (
                                    <option key={creditor.key} value={creditor.key}>{creditor.value}</option>
                                ))}
                            </Field>
                            <ErrorMessage name='expenseCreditor' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                        </Form.Field>
                        <Form.Field>
                            <label>Type</label>
                            <Field as="select" name="expenseType">
                                <option value={-1}>Select Type</option>
                                {ExpenseTypes.map(type => (
                                    <option key={type.key} value={type.key}>{type.value}</option>
                                ))}
                            </Field>
                            <ErrorMessage name='expenseType' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                        </Form.Field>
                        {expenseItem && (
                            <Form.Field>
                                <label>Amount: €{expenseItem.amount.toFixed(2)}</label>
                                <p style={{ color: '#666', fontSize: '0.9em' }}>
                                    <em>Amount is calculated from line items. Expand the expense item to add or edit line items.</em>
                                </p>
                            </Form.Field>
                        )}
                        {!expenseItem && (
                            <p style={{ color: '#666', fontSize: '0.9em', fontStyle: 'italic' }}>
                                Note: After creating the expense item, expand it to add line items.
                            </p>
                        )}
                        <Button
                            disabled={isSubmitting || !dirty || !isValid}
                            loading={loading} floated='right'
                            positive type='submit' content='Submit' />
                        <Button onClick={closeForm} floated='right' type='button' content='Cancel' />
                    </FormikForm>
                )}
            </Formik>
        </Segment>
    )
})
