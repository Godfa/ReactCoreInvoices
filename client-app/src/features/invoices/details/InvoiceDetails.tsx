import React from "react";
import { Button, Card, Image, List } from "semantic-ui-react";
import LoadingComponent from "../../../app/layout/LoadingComponent";
import { useStore } from "../../../app/stores/store";
import ExpenseItemList from "./ExpenseItemList";
import ParticipantList from "./ParticipantList";

export default function InvoiceDetails() {
    const { invoiceStore } = useStore();
    const { selectedInvoice: invoice, openForm, cancelSelectedInvoice } = invoiceStore;

    if (!invoice) return <LoadingComponent />;
    return (
        <Card fluid>
            <Image src={`/assets/lanImages/${invoice.image}.jpg`} />
            <Card.Content>
                <Card.Header>{invoice.title}</Card.Header>
                <Card.Meta>
                    <span>LAN #{invoice.lanNumber}</span>
                </Card.Meta>
                <Card.Meta>
                    <span className="amount">{invoice.amount}€</span>
                </Card.Meta>
                <Card.Description>
                    {invoice.description}
                </Card.Description>
            </Card.Content>
            <Card.Content>
                <ParticipantList invoiceId={invoice.id} />
            </Card.Content>
            <Card.Content>
                <ExpenseItemList invoiceId={invoice.id} />
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