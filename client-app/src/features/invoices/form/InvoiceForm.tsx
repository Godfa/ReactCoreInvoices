import { observer } from "mobx-react-lite";
import React, { ChangeEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, Form, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";


export default observer(function InvoiceForm() {
    const navigate = useNavigate();
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
        // No required fields - both title and description are optional
        return true;
    }

    async function handleSubmit() {
        if (!validateForm()) return;
        if (invoice.id) {
            await updateInvoice(invoice);
            navigate(`/invoices/${invoice.id}`);
        } else {
            await createInvoice(invoice);
            // Navigate to the newly created invoice (selectedInvoice is updated by createInvoice)
            if (invoiceStore.selectedInvoice) {
                navigate(`/invoices/${invoiceStore.selectedInvoice.id}`);
            }
        }
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
                    placeholder='Title (optional - auto-generated if empty)'
                    value={invoice.title}
                    name='title'
                    onChange={handleInputChange}
                />
                <Form.Input
                    placeholder='Description (optional)'
                    value={invoice.description}
                    name='description'
                    onChange={handleInputChange}
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