
import { observer } from "mobx-react-lite";
import React, { SyntheticEvent, useState } from "react";
import { Button, Item, ItemDescription, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { Link } from "react-router-dom";


export default observer(function InvoiceList() {
    const { invoiceStore } = useStore();
    const { deleteInvoice, Invoices, loading } = invoiceStore;

    const [target, setTarget] = useState('');

    function handleInvoiceDelete(e: SyntheticEvent<HTMLButtonElement>, id: string) {
        setTarget(e.currentTarget.name);
        deleteInvoice(id);
    }


    return (
        <Segment>
            <Item.Group divided>
                {Invoices.map(invoice => (
                    <Item key={invoice.id}>
                        <Item.Content>
                            <Item.Header as='a'>{invoice.title}</Item.Header>
                            <Item.Meta>
                                LAN #{invoice.lanNumber} • {invoice.amount}€
                            </Item.Meta>
                            <ItemDescription>
                                <div>{invoice.description}</div>
                            </ItemDescription>
                            <Item.Extra>
                                <Button as={Link} to={`/invoices/${invoice.id}`} floated='right' content='View' color='blue' />
                                <Button
                                    name={invoice.id}
                                    loading={loading && target === invoice.id}
                                    onClick={(e) => handleInvoiceDelete(e, invoice.id)}
                                    floated='right' content='Delete' color='red' />
                            </Item.Extra>
                        </Item.Content>
                    </Item>
                ))}
            </Item.Group>
        </Segment>
    )


})