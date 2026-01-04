import React, { useEffect } from "react";
import { Button, Card, Image } from "semantic-ui-react";
import LoadingComponent from "../../../app/layout/LoadingComponent";
import { useStore } from "../../../app/stores/store";
import ExpenseItemList from "./ExpenseItemList";
import ParticipantList from "./ParticipantList";
import { Link, useParams } from "react-router-dom";
import { observer } from "mobx-react-lite";

export default observer(function InvoiceDetails() {
    const { invoiceStore } = useStore();
    const { selectedInvoice: invoice, loadInvoice, loadingInitial, cancelSelectedInvoice } = invoiceStore;
    const { id } = useParams<{ id: string }>();

    useEffect(() => {
        if (id) {
            loadInvoice(id);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id]);

    if (loadingInitial || !invoice) return <LoadingComponent />;

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
                    <Button as={Link} to={`/manage/${invoice.id}`} basic color='blue' content='Edit' />
                    <Button as={Link} to='/invoices' basic color='grey' content='Cancel' />
                </Button.Group>
            </Card.Content>
        </Card>
    )
})