import { observer } from "mobx-react-lite";
import React from "react";
import { Button, Form, Header, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Formik, Form as FormikForm, Field, ErrorMessage } from "formik";
import * as Yup from 'yup';
import { v4 as uuid } from 'uuid';
import { ExpenseLineItem } from "Invoices";

interface Props {
    invoiceId: string;
    expenseItemId: string;
    closeForm: () => void;
    lineItem?: ExpenseLineItem;
}

interface FormValues {
    name: string;
    quantity: number;
    unitPrice: number;
    id: string;
    expenseItemId: string;
}

export default observer(function ExpenseLineItemForm({ invoiceId, expenseItemId, closeForm, lineItem }: Props) {
    const { invoiceStore } = useStore();
    const { createLineItem, updateLineItem, loading } = invoiceStore;

    const validationSchema = Yup.object({
        name: Yup.string().required('Name is required').max(200, 'Name must not exceed 200 characters'),
        quantity: Yup.number().required('Quantity is required').positive('Quantity must be greater than 0').integer('Quantity must be an integer'),
        unitPrice: Yup.number().required('Unit Price is required').min(0, 'Unit Price must be greater than or equal to 0')
    })

    const initialValues: FormValues = lineItem ? {
        name: lineItem.name,
        quantity: lineItem.quantity,
        unitPrice: lineItem.unitPrice,
        id: lineItem.id,
        expenseItemId: lineItem.expenseItemId
    } : {
        name: '',
        quantity: 1,
        unitPrice: 0,
        id: '',
        expenseItemId: expenseItemId
    }

    return (
        <Segment clearing>
            <Header content={lineItem ? 'Edit Line Item' : 'Add Line Item'} sub color='teal' />
            <Formik
                initialValues={initialValues}
                onSubmit={(values) => {
                    const lineItemData: ExpenseLineItem = {
                        ...values,
                        id: lineItem ? lineItem.id : uuid(),
                        expenseItemId: expenseItemId,
                        total: values.quantity * values.unitPrice
                    };
                    const action = lineItem
                        ? updateLineItem(invoiceId, expenseItemId, lineItemData)
                        : createLineItem(invoiceId, expenseItemId, lineItemData);
                    return action.then(() => closeForm());
                }}
                validationSchema={validationSchema}
            >
                {({ handleSubmit, isValid, isSubmitting, dirty, values }) => (
                    <FormikForm className='ui form' onSubmit={handleSubmit} autoComplete='off'>
                        <Form.Field>
                            <label>Name</label>
                            <Field placeholder='Item name' name='name' />
                            <ErrorMessage name='name' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                        </Form.Field>
                        <Form.Field>
                            <label>Quantity</label>
                            <Field type='number' step='1' placeholder='Quantity' name='quantity' />
                            <ErrorMessage name='quantity' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                        </Form.Field>
                        <Form.Field>
                            <label>Unit Price (€)</label>
                            <Field type='number' step='0.01' placeholder='Unit Price' name='unitPrice' />
                            <ErrorMessage name='unitPrice' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                        </Form.Field>
                        <Form.Field>
                            <label>Total: €{(values.quantity * values.unitPrice).toFixed(2)}</label>
                        </Form.Field>
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
