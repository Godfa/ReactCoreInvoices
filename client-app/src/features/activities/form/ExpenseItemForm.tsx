import { observer } from "mobx-react-lite";
import React, { useEffect } from "react";
import { Button, Form, Header, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Formik, Form as FormikForm, Field, ErrorMessage } from "formik";
import * as Yup from 'yup';
import { v4 as uuid } from 'uuid';

interface Props {
    invoiceId: string;
    closeForm: () => void;
}

interface FormValues {
    name: string;
    expenseType: number;
    expenseCreditor: number;
    id: string;
}

export default observer(function ExpenseItemForm({ invoiceId, closeForm }: Props) {
    const { invoiceStore } = useStore();
    const { createExpenseItem, loading, loadExpenseTypes, ExpenseTypes } = invoiceStore;

    useEffect(() => {
        loadExpenseTypes();
    }, [loadExpenseTypes]);

    const validationSchema = Yup.object({
        name: Yup.string().required('The event name is required'),
        expenseType: Yup.number().required('Expense Type is required').notOneOf([-1], 'Type is required'),
        expenseCreditor: Yup.number().required('Creditor is required')
    })

    const initialValues: FormValues = {
        name: '',
        expenseType: -1, // Changed default to -1 to match placeholder
        expenseCreditor: 0,
        id: ''
    }

    return (
        <Segment clearing>
            <Header content='Add Expense Item' sub color='teal' />
            <Formik
                initialValues={initialValues}
                onSubmit={(values) => createExpenseItem(invoiceId, {
                    ...values,
                    expenseType: parseInt(values.expenseType.toString()),
                    expenseCreditor: parseInt(values.expenseCreditor.toString()),
                    id: uuid()
                }).then(() => closeForm())}
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
                            <label>Creditor (Number)</label>
                            <Field placeholder='Creditor' name='expenseCreditor' type='number' />
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
