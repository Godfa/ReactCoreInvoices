import { observer } from "mobx-react-lite";
import React, { useEffect, useState } from "react";
import { Button, Form, Header, Segment, Icon, Checkbox } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Formik, Form as FormikForm, Field, ErrorMessage } from "formik";
import * as Yup from 'yup';
import { v4 as uuid } from 'uuid';
import { ExpenseItem, ExpenseLineItem } from "../../../app/models/invoice";

interface Props {
    invoiceId: string;
    closeForm: () => void;
    expenseItem?: ExpenseItem;
}

interface FormValues {
    name: string;
    expenseType: number;
    organizerId: string;
    id: string;
    payers: any[];
    lineItems: any[];
    // Optional first line item fields
    lineItemName?: string;
    lineItemQuantity?: number;
    lineItemUnitPrice?: number;
}

export default observer(function ExpenseItemForm({ invoiceId, closeForm, expenseItem }: Props) {
    const { invoiceStore } = useStore();
    const { createExpenseItem, updateExpenseItem, createLineItem, loading, loadExpenseTypes, loadUsers, ExpenseTypes, PotentialParticipants } = invoiceStore;
    const [addLineItem, setAddLineItem] = useState(false);

    useEffect(() => {
        loadExpenseTypes();
        loadUsers();
    }, [loadExpenseTypes, loadUsers]);

    const validationSchema = Yup.object({
        name: Yup.string().required('The event name is required'),
        expenseType: Yup.number().required('Expense Type is required').notOneOf([-1], 'Type is required'),
        organizerId: Yup.string().required('Velkoja on pakollinen'),
        // Conditional validation for line item fields
        lineItemName: Yup.string().when('$addLineItem', {
            is: true,
            then: (schema) => schema.required('Line item name is required'),
            otherwise: (schema) => schema.notRequired()
        }),
        lineItemQuantity: Yup.number().when('$addLineItem', {
            is: true,
            then: (schema) => schema.required('Quantity is required').positive('Must be positive'),
            otherwise: (schema) => schema.notRequired()
        }),
        lineItemUnitPrice: Yup.number().when('$addLineItem', {
            is: true,
            then: (schema) => schema.required('Unit price is required').min(0, 'Must be zero or positive'),
            otherwise: (schema) => schema.notRequired()
        })
    })

    const initialValues: FormValues = expenseItem ? {
        name: expenseItem.name,
        expenseType: expenseItem.expenseType,
        organizerId: expenseItem.organizerId,
        id: expenseItem.id,
        payers: expenseItem.payers || [],
        lineItems: expenseItem.lineItems || []
    } : {
        name: '',
        expenseType: -1,
        organizerId: '',
        id: '',
        payers: [],
        lineItems: [],
        lineItemName: 'Ostokset',
        lineItemQuantity: 1,
        lineItemUnitPrice: 0
    }

    return (
        <Segment clearing>
            <Header content={expenseItem ? 'Edit Expense Item' : 'Add Expense Item'} sub color='teal' />
            <Formik
                initialValues={initialValues}
                context={{ addLineItem }}
                onSubmit={async (values) => {
                    const expenseItemId = expenseItem ? expenseItem.id : uuid();
                    const organizer = PotentialParticipants.find(p => p.key === values.organizerId);

                    const itemData = {
                        ...values,
                        expenseType: parseInt(values.expenseType.toString()),
                        organizerId: values.organizerId,
                        organizer: organizer ? { id: organizer.key, displayName: organizer.value, userName: '', email: '' } : undefined,
                        id: expenseItemId
                    } as any;
                    // Cast to any to avoid strict type checking on partial user object if needed, 
                    // though we should match the interface. The Organizer object is needed for UI display if not refreshed.

                    // Create or update the expense item
                    if (expenseItem) {
                        await updateExpenseItem(invoiceId, itemData);
                    } else {
                        await createExpenseItem(invoiceId, itemData);

                        // If adding a line item, create it after expense item is created
                        if (addLineItem && values.lineItemName && values.lineItemQuantity && values.lineItemUnitPrice !== undefined) {
                            const lineItem: ExpenseLineItem = {
                                id: uuid(),
                                expenseItemId: expenseItemId,
                                name: values.lineItemName,
                                quantity: values.lineItemQuantity,
                                unitPrice: values.lineItemUnitPrice,
                                total: values.lineItemQuantity * values.lineItemUnitPrice
                            };
                            await createLineItem(invoiceId, expenseItemId, lineItem);
                        }
                    }

                    closeForm();
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
                            <label>Velkoja</label>
                            <Field as="select" name="organizerId">
                                <option value="">Valitse velkoja</option>
                                {PotentialParticipants.map(user => (
                                    <option key={user.key} value={user.key}>{user.value}</option>
                                ))}
                            </Field>
                            <ErrorMessage name='organizerId' render={error => <label style={{ color: 'red' }}>{error}</label>} />
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
                                <label>Amount: €{(expenseItem.lineItems?.reduce((sum, li) => sum + li.quantity * li.unitPrice, 0) ?? 0).toFixed(2)}</label>
                                <p style={{ color: '#666', fontSize: '0.9em' }}>
                                    <em>Amount is calculated from line items. Expand the expense item to add or edit line items.</em>
                                </p>
                            </Form.Field>
                        )}
                        {!expenseItem && (
                            <>
                                <Form.Field>
                                    <Checkbox
                                        label='Lisää rivi suoraan'
                                        checked={addLineItem}
                                        onChange={(e, { checked }) => setAddLineItem(!!checked)}
                                    />
                                </Form.Field>
                                {addLineItem && (
                                    <Segment>
                                        <Header size='small' content='Rivin tiedot' />
                                        <Form.Field>
                                            <label>Nimi</label>
                                            <Field placeholder='Esim. Olut, Pizza, jne.' name='lineItemName' />
                                            <ErrorMessage name='lineItemName' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                                        </Form.Field>
                                        <Form.Group widths='equal'>
                                            <Form.Field>
                                                <label>Määrä</label>
                                                <Field type='number' step='0.01' placeholder='Määrä' name='lineItemQuantity' />
                                                <ErrorMessage name='lineItemQuantity' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                                            </Form.Field>
                                            <Form.Field>
                                                <label>Yksikköhinta (€)</label>
                                                <Field type='number' step='0.01' placeholder='Hinta' name='lineItemUnitPrice' />
                                                <ErrorMessage name='lineItemUnitPrice' render={error => <label style={{ color: 'red' }}>{error}</label>} />
                                            </Form.Field>
                                        </Form.Group>
                                    </Segment>
                                )}
                                {!addLineItem && (
                                    <p style={{ color: '#666', fontSize: '0.9em', fontStyle: 'italic' }}>
                                        Voit lisätä rivejä myös jälkeenpäin laajentamalla kuluerän.
                                    </p>
                                )}
                            </>
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
