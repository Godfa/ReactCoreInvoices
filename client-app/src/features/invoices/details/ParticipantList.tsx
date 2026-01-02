import { observer } from "mobx-react-lite";
import React, { useEffect } from "react";
import { Button, Header, Label, Segment } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";

interface Props {
    invoiceId: string;
}

export default observer(function ParticipantList({ invoiceId }: Props) {
    const { invoiceStore } = useStore();
    const { selectedInvoice, addParticipant, removeParticipant, loadCreditors, Creditors, loading } = invoiceStore;

    useEffect(() => {
        loadCreditors();
    }, [loadCreditors]);

    if (!selectedInvoice) return null;

    const participants = selectedInvoice.participants || [];
    const participantIds = participants.map(p => p.creditorId);

    const handleAddParticipant = async (creditorId: number) => {
        await addParticipant(invoiceId, creditorId);
    };

    const handleRemoveParticipant = async (creditorId: number) => {
        await removeParticipant(invoiceId, creditorId);
    };

    return (
        <Segment>
            <Header as='h3'>Participants</Header>
            <div style={{ marginBottom: '10px' }}>
                {participants.length > 0 ? (
                    participants.map(p => (
                        <Label key={p.creditorId} style={{ marginRight: '5px', marginBottom: '5px' }}>
                            {p.creditor.name}
                            <Label.Detail>
                                <Button
                                    size='mini'
                                    icon='delete'
                                    onClick={() => handleRemoveParticipant(p.creditorId)}
                                    loading={loading}
                                    disabled={loading}
                                    style={{ marginLeft: '5px', padding: '3px' }}
                                />
                            </Label.Detail>
                        </Label>
                    ))
                ) : (
                    <p style={{ color: '#999' }}>No participants yet</p>
                )}
            </div>
            <Header as='h4'>Add Participant</Header>
            <div>
                {Creditors
                    .filter(c => !participantIds.includes(c.key))
                    .map(creditor => (
                        <Button
                            key={creditor.key}
                            size='tiny'
                            color='teal'
                            basic
                            content={creditor.value}
                            onClick={() => handleAddParticipant(creditor.key)}
                            loading={loading}
                            disabled={loading}
                            style={{ marginRight: '5px', marginBottom: '5px' }}
                        />
                    ))}
            </div>
        </Segment>
    );
});
