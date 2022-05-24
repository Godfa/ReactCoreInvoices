import { Invoice } from "Invoices";
import React from "react";
import { Button, Card, Image } from "semantic-ui-react";


interface Props {
    invoice: Invoice;
    cancelSelectInvoice: () => void;
    openForm: (id: string) => void;
}
export default function InvoiceDetails({invoice, cancelSelectInvoice, openForm }: Props) {
    return (
        <Card fluid>
            <Image src={`/assets/lanImages/${invoice.image}.jpg`} />
            <Card.Content>
                <Card.Header>{invoice.title}</Card.Header>
                <Card.Meta>
                    <span>{invoice.amount}€</span>
                </Card.Meta>
                <Card.Description>
                   {invoice.description}
                </Card.Description>
            </Card.Content>
            <Card.Content extra>
               <Button.Group width='2'>
                   <Button onClick={() => openForm(invoice.id)} basic color='blue' content='Edit' />
                   <Button onClick={cancelSelectInvoice} basic color='grey' content='Cancel' />
               </Button.Group>
            </Card.Content>
        </Card>
    )
}