import { Invoice } from "Invoices";
import React, { ChangeEvent, useState } from "react";
import { Button, Form, Segment } from "semantic-ui-react";

interface Props {
    invoice: Invoice | undefined;
    closeForm: () => void;
    createOrEdit: (invoice: Invoice) => void;
    submitting: boolean;
}

export default function InvoiceForm({invoice: selectedInvoice, closeForm, createOrEdit, submitting}: Props) {

    const initialState = selectedInvoice ?? {
        id: '',
        description: '',
        title: '',
        amount: 0,
        image: '',
        lanNumber: '',
        expenseItems: []
    }

    const [invoice, setInvoice] = useState(initialState);

    function handleSubmit() {
       createOrEdit(invoice);
    }

    function handleInputChange(event: ChangeEvent<HTMLInputElement>) {
        const {name, value} = event.target;
        setInvoice({...invoice,[name]:value})
    }
    return (
        <Segment clearing>
            <Form onSubmit={handleSubmit} autoComplete= 'off'>
                <Form.Input placeholder='Title' value={invoice.title} name='title' 
                onChange={handleInputChange}/>
                <Form.Input placeholder='Description' value={invoice.description} name='description' 
                onChange={handleInputChange}/>
                <Form.Input placeholder='Amount' value={invoice.amount} name='amount' 
                onChange={handleInputChange}/>
                <Form.Input placeholder='Lan number' value={invoice.lanNumber} name='lanNumber' 
                onChange={handleInputChange}/> 
                <Button loading={submitting} floated='right' positive type='submit' content='Submit' value={invoice.title} name='title' 
                onChange={handleInputChange}/>
                <Button onClick={closeForm} floated='right' type='button' content='Cancel'/>
            </Form>
        </Segment>
    )
}