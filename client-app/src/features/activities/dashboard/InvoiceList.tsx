import { Invoice } from "Invoices";
import React from "react";
import { Button, Item, ItemDescription, Segment } from "semantic-ui-react";

interface Props {
    invoices: Invoice[];
    selectInvoice: (id: string) => void;
    deleteInvoice: (id: string) => void;
     
}

export default function InvoiceList({invoices, selectInvoice, deleteInvoice}:Props) {
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
                                <Button onClick={() => deleteInvoice(invoice.id)} floated='right' content='Delete' color='red' />
                                {/* <Label basic content={invoice.amount} /> */}
                            </Item.Extra>
                        </Item.Content>
                    </Item>
                ))}
            </Item.Group>
        </Segment>
    )


}