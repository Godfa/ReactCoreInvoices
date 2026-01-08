import { observer } from "mobx-react-lite";
import React, { ChangeEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, Form, Icon } from "semantic-ui-react";
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

    const isEditing = !!invoice.id;

    return (
        <div className="animate-fade-in">
            <h1 style={{ marginBottom: 'var(--spacing-xl)' }}>
                {isEditing ? 'Muokkaa laskua' : 'Luo uusi lasku'}
            </h1>

            <div className="glass-card" style={{ padding: 'var(--spacing-xl)' }}>
                <Form onSubmit={handleSubmit} autoComplete='off'>
                    <Form.Field>
                        <label>Otsikko</label>
                        <Form.Input
                            placeholder='Otsikko (valinnainen - luodaan automaattisesti jos tyhjä)'
                            value={invoice.title}
                            name='title'
                            onChange={handleInputChange}
                            icon='file alternate outline'
                            iconPosition='left'
                        />
                    </Form.Field>

                    <Form.Field>
                        <label>Kuvaus</label>
                        <Form.Input
                            placeholder='Kuvaus (valinnainen)'
                            value={invoice.description}
                            name='description'
                            onChange={handleInputChange}
                            icon='align left'
                            iconPosition='left'
                        />
                    </Form.Field>

                    <div style={{ display: 'flex', gap: 'var(--spacing-md)', marginTop: 'var(--spacing-xl)', flexWrap: 'wrap' }}>
                        <Button
                            loading={loading}
                            className="btn-primary"
                            type='submit'
                        >
                            <Icon name="check" /> {isEditing ? 'Tallenna' : 'Luo lasku'}
                        </Button>
                        <Button onClick={closeForm} className="btn-secondary" type='button'>
                            <Icon name="cancel" /> Peruuta
                        </Button>
                    </div>
                </Form>
            </div>
        </div>
    )
})
