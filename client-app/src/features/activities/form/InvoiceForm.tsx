import { observer } from "mobx-react-lite";
import React, { ChangeEvent, useState } from "react";
import { Button, Form, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";


export default observer(function InvoiceForm() {

    const { invoiceStore } = useStore();
    const { selectedInvoice, closeForm, createInvoice, updateInvoice, loading } = invoiceStore;

    const initialState = selectedInvoice ?? {
        id: '',
        description: '',
        title: '',
        amount: 0,
        image: '',
        lanNumber: 0,
        expenseItems: []
    }

    const [invoice, setInvoice] = useState(initialState);
    const [errors, setErrors] = useState<{ [key: string]: string }>({});

    function validateForm() {
        const newErrors: { [key: string]: string } = {};

        if (!invoice.title || invoice.title.trim() === '') {
            newErrors.title = 'Title is required';
        }

        if (!invoice.description || invoice.description.trim() === '') {
            newErrors.description = 'Description is required';
        }

        if (invoice.amount <= 0) {
            newErrors.amount = 'Amount must be greater than 0';
        }

        if (invoice.lanNumber <= 0) {
            newErrors.lanNumber = 'LAN number must be greater than 0';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    }

    function handleSubmit() {
        if (!validateForm()) return;
        invoice.id ? updateInvoice(invoice) : createInvoice(invoice);
    }

    function handleInputChange(event: ChangeEvent<HTMLInputElement>) {
        const { name, value } = event.target;
        setInvoice({ ...invoice, [name]: value })
        // Clear error for this field when user starts typing
        if (errors[name]) {
            setErrors({ ...errors, [name]: '' });
        }
    }

    return (
        <Segment clearing>
            <Form onSubmit={handleSubmit} autoComplete='off'>
                <Form.Input
                    placeholder='Title'
                    value={invoice.title}
                    name='title'
                    onChange={handleInputChange}
                    error={errors.title ? { content: errors.title, pointing: 'below' } : null}
                />
                <Form.Input
                    placeholder='Description'
                    value={invoice.description}
                    name='description'
                    onChange={handleInputChange}
                    error={errors.description ? { content: errors.description, pointing: 'below' } : null}
                />
                <Form.Input
                    placeholder='Amount'
                    value={invoice.amount}
                    name='amount'
                    type='number'
                    step='0.01'
                    onChange={handleInputChange}
                    error={errors.amount ? { content: errors.amount, pointing: 'below' } : null}
                />
                <Form.Input
                    placeholder='LAN number'
                    value={invoice.lanNumber}
                    name='lanNumber'
                    type='number'
                    onChange={handleInputChange}
                    error={errors.lanNumber ? { content: errors.lanNumber, pointing: 'below' } : null}
                />
                <Button
                    loading={loading}
                    floated='right'
                    positive
                    type='submit'
                    content='Submit'
                />
                <Button onClick={closeForm} floated='right' type='button' content='Cancel' />
            </Form>
        </Segment>
    )
})