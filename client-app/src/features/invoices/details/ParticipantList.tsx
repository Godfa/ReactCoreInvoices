import { observer } from "mobx-react-lite";
import React, { useEffect } from "react";
import { Button, Icon } from "semantic-ui-react";
import { useStore } from "../../../app/stores/store";
import { toast } from "react-toastify";

interface Props {
    invoiceId: string;
}

export default observer(function ParticipantList({ invoiceId }: Props) {
    const { invoiceStore } = useStore();
    const { selectedInvoice, addParticipant, removeParticipant, loadCreditors, Creditors, loading, addUsualSuspects } = invoiceStore;

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

    const handleAddAllParticipants = async () => {
        invoiceStore.loading = true;
        try {
            const nonParticipants = Creditors.filter(c => !participantIds.includes(c.key));
            await Promise.all(
                nonParticipants.map(creditor =>
                    addParticipant(invoiceId, creditor.key, true)
                )
            );
            if (nonParticipants.length > 0) {
                toast.success(`${nonParticipants.length} osallistujaa lisätty`);
            }
        } finally {
            invoiceStore.loading = false;
        }
    };

    const handleAddUsualSuspects = async () => {
        await addUsualSuspects(invoiceId);
    };

    const handleRemoveAllParticipants = async () => {
        invoiceStore.loading = true;
        try {
            await Promise.all(
                participants.map(p =>
                    removeParticipant(invoiceId, p.creditorId, true)
                )
            );
            toast.success('Kaikki osallistujat poistettu');
        } finally {
            invoiceStore.loading = false;
        }
    };

    const nonParticipantCount = Creditors.filter(c => !participantIds.includes(c.key)).length;
    const usualSuspects = ['Epi', 'JHattu', 'Leivo', 'Timo', 'Jaapu', 'Urpi', 'Zeip'];
    const usualSuspectsToAdd = Creditors.filter(c =>
        usualSuspects.includes(c.value) && !participantIds.includes(c.key)
    ).length;

    const getInitials = (name: string) => {
        return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
    };

    return (
        <div className="glass-card" style={{ padding: 'var(--spacing-lg)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--spacing-md)', flexWrap: 'wrap', gap: 'var(--spacing-sm)' }}>
                <h3 style={{ margin: 0 }}>Osallistujat</h3>
                <div style={{ display: 'flex', gap: 'var(--spacing-sm)', flexWrap: 'wrap' }}>
                    {participants.length > 0 && (
                        <Button
                            size='tiny'
                            className='btn-danger'
                            onClick={handleRemoveAllParticipants}
                            loading={loading}
                            disabled={loading}
                        >
                            <Icon name='trash' /> Poista kaikki
                        </Button>
                    )}
                    {usualSuspectsToAdd > 0 && (
                        <Button
                            size='tiny'
                            className='btn-primary'
                            onClick={handleAddUsualSuspects}
                            loading={loading}
                            disabled={loading}
                        >
                            <Icon name='users' /> Vakkarit ({usualSuspectsToAdd})
                        </Button>
                    )}
                    {nonParticipantCount > 0 && (
                        <Button
                            size='tiny'
                            className='btn-success'
                            onClick={handleAddAllParticipants}
                            loading={loading}
                            disabled={loading}
                        >
                            <Icon name='plus' /> Lisää kaikki ({nonParticipantCount})
                        </Button>
                    )}
                </div>
            </div>

            {/* Current Participants */}
            <div className="participant-grid" style={{ marginBottom: 'var(--spacing-lg)' }}>
                {participants.length > 0 ? (
                    participants.map(p => (
                        <div key={p.creditorId} className="participant-card">
                            <div className="participant-avatar">
                                {getInitials(p.creditor.name)}
                            </div>
                            <span className="participant-name">{p.creditor.name}</span>
                            <Button
                                size='mini'
                                icon='close'
                                onClick={() => handleRemoveParticipant(p.creditorId)}
                                loading={loading}
                                disabled={loading}
                                style={{ marginLeft: 'auto', padding: '4px', background: 'transparent', color: 'var(--text-muted)' }}
                            />
                        </div>
                    ))
                ) : (
                    <p style={{ color: 'var(--text-muted)' }}>Ei osallistujia vielä</p>
                )}
            </div>

            {/* Add Participants */}
            {Creditors.filter(c => !participantIds.includes(c.key)).length > 0 && (
                <>
                    <h4 style={{ marginBottom: 'var(--spacing-sm)' }}>Lisää osallistuja</h4>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--spacing-sm)' }}>
                        {Creditors
                            .filter(c => !participantIds.includes(c.key))
                            .map(creditor => (
                                <Button
                                    key={creditor.key}
                                    size='tiny'
                                    className='btn-secondary'
                                    onClick={() => handleAddParticipant(creditor.key)}
                                    loading={loading}
                                    disabled={loading}
                                >
                                    <Icon name='plus' /> {creditor.value}
                                </Button>
                            ))}
                    </div>
                </>
            )}
        </div>
    );
});
