import { Invoice } from "Invoices";
import React, { SyntheticEvent, useState } from "react";
import { Button, Item, ItemDescription, Segment } from "semantic-ui-react";

interface Props {
    invoices: Invoice[];
    selectInvoice: (id: string) => void;
    deleteInvoice: (id: string) => void;
    submitting: boolean;
     
}

export default function InvoiceList({invoices, selectInvoice, deleteInvoice, submitting}:Props) {
    const [target, setTarget] = useState('');

    function handleInvoiceDelete(e:SyntheticEvent<HTMLButtonElement>, id: string) {
        setTarget(e.currentTarget.name);
        deleteInvoice(id);
    }
    return (
        <Segment>
            <Item.Group divided>
                {invoices.map(invoice => (
                    <Item key= {invoice.id}>
                        <Item.Content>
                            <Item.Header as='a'>{invoice.title}</Item.Header>
                            {/* <Item.Meta>
                                {invoice.lanNumber}
                            </Item.Meta> */}
                            <ItemDescription>
                                <div>{invoice.description}</div>
                            </ItemDescription>
                            <Item.Extra>
                                <Button onClick={() => selectInvoice(invoice.id)} floated='right' content='View' color='blue' />
                                <Button
                                    name={invoice.id} 
                                    loading={submitting && target === invoice.id} 
                                    onClick={(e) => handleInvoiceDelete(e,invoice.id)} 
                                    floated='right' content='Delete' color='red' />
                                {/* <Label basic content={invoice.amount} /> */}
                            </Item.Extra>
                        </Item.Content>
                    </Item>
                ))}
            </Item.Group>
        </Segment>
    )


}