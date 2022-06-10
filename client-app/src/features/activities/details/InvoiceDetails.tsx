import React from "react";
import { Button, Card, Image } from "semantic-ui-react";
import LoadingComponent from "../../../app/layout/LoadingComponent";
import { useStore } from "../../../app/stores/store";

export default function InvoiceDetails() {
    const {invoiceStore} = useStore();
    const {selectedInvoice: invoice, openForm, cancelSelectedInvoice} = invoiceStore;

    if (!invoice) return <LoadingComponent />;
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
                   <Button onClick={cancelSelectedInvoice} basic color='grey' content='Cancel' />
               </Button.Group>
            </Card.Content>
        </Card>
    )
}