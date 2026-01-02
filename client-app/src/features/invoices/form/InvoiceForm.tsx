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
        image: '',
        expenseItems: [],
        participants: []
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