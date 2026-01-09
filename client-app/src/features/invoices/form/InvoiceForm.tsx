import { observer } from "mobx-react-lite";
import React, { ChangeEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, Form, Icon, Message } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { InvoiceStatus } from "../../../app/models/invoice";


export default observer(function InvoiceForm() {
    const navigate = useNavigate();
    const { invoiceStore } = useStore();
    const { selectedInvoice, closeForm, createInvoice, updateInvoice, loading, canCreateInvoice } = invoiceStore;

    const initialState = selectedInvoice ?? {
        id: '00000000-0000-0000-0000-000000000000',
        lanNumber: 0,
        description: '',
        title: '',
        image: '',
        status: InvoiceStatus.Aktiivinen,
        amount: 0,
        expenseItems: [],
        participants: []
    }

    const [invoice, setInvoice] = useState(initialState);
    const [errors, setErrors] = useState<{ [key: string]: string }>({});
    const [createShoppingExpense, setCreateShoppingExpense] = useState(false);
    const [shoppingPrice, setShoppingPrice] = useState('');

    useEffect(() => {
        if (!invoice.id) {
            setCreateShoppingExpense(false);
            setShoppingPrice('');
        }
    }, [invoice.id]);

    function validateForm() {
        // No required fields - both title and description are optional
        return true;
    }

    async function handleSubmit() {
        if (!validateForm()) return;
        if (isEditing) {
            await updateInvoice(invoice);
            navigate(`/invoices/${invoice.id}`);
        } else {
            const shoppingData = createShoppingExpense && shoppingPrice ? {
                price: parseFloat(shoppingPrice),
                enabled: true
            } : null;

            await createInvoice(invoice, shoppingData);
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

    const isEditing = !!invoice.id && invoice.id !== '00000000-0000-0000-0000-000000000000';

    return (
        <div className="animate-fade-in">
            <h1 style={{ marginBottom: 'var(--spacing-xl)' }}>
                {isEditing ? 'Muokkaa laskua' : 'Luo uusi lasku'}
            </h1>

            {!isEditing && !canCreateInvoice && (
                <Message warning>
                    <Message.Header>Uutta laskua ei voi luoda</Message.Header>
                    <p>Olemassa oleva lasku on aktiivinen tai katselmoitavana. Arkistoi ensin nykyinen lasku ennen uuden luomista.</p>
                </Message>
            )}

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

                    {!isEditing && (
                        <Form.Field>
                            <div style={{
                                padding: 'var(--spacing-md)',
                                background: 'var(--surface-secondary)',
                                borderRadius: 'var(--radius-md)',
                                marginTop: 'var(--spacing-md)'
                            }}>
                                <Form.Checkbox
                                    label='Lisää "Ostokset" kuluerä'
                                    checked={createShoppingExpense}
                                    onChange={(e, { checked }) => {
                                        setCreateShoppingExpense(checked || false);
                                        if (!checked) setShoppingPrice('');
                                    }}
                                />

                                {createShoppingExpense && (
                                    <Form.Field style={{ marginTop: 'var(--spacing-md)' }}>
                                        <label>Ostokset hinta (€)</label>
                                        <Form.Input
                                            placeholder='Esim. 150.50'
                                            value={shoppingPrice}
                                            name='shoppingPrice'
                                            type='number'
                                            step='0.01'
                                            min='0.01'
                                            onChange={(e) => setShoppingPrice(e.target.value)}
                                            icon='euro sign'
                                            iconPosition='left'
                                        />
                                    </Form.Field>
                                )}
                            </div>
                        </Form.Field>
                    )}

                    <div style={{ display: 'flex', gap: 'var(--spacing-md)', marginTop: 'var(--spacing-xl)', flexWrap: 'wrap' }}>
                        <Button
                            loading={loading}
                            className="btn-primary"
                            type='submit'
                            disabled={!isEditing && !canCreateInvoice}
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
