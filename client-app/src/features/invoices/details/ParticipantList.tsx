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
    const { selectedInvoice, addParticipant, removeParticipant, loadUsers, PotentialParticipants, loading, addUsualSuspects } = invoiceStore;

    useEffect(() => {
        loadUsers();
    }, [loadUsers]);

    if (!selectedInvoice) return null;

    const participants = selectedInvoice.participants || [];
    const participantIds = participants.map(p => p.appUserId);

    const handleAddParticipant = async (userId: string) => {
        await addParticipant(invoiceId, userId);
    };

    const handleRemoveParticipant = async (userId: string) => {
        await removeParticipant(invoiceId, userId);
    };

    const handleAddAllParticipants = async () => {
        invoiceStore.loading = true;
        try {
            const nonParticipants = PotentialParticipants.filter(c => !participantIds.includes(c.key));
            await Promise.all(
                nonParticipants.map(user =>
                    addParticipant(invoiceId, user.key, true)
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
                    removeParticipant(invoiceId, p.appUserId, true)
                )
            );
            toast.success('Kaikki osallistujat poistettu');
        } finally {
            invoiceStore.loading = false;
        }
    };

    const nonParticipantCount = PotentialParticipants.filter(c => !participantIds.includes(c.key)).length;
    const usualSuspects = ['Epi', 'JHattu', 'Leivo', 'Timo', 'Jaapu', 'Urpi', 'Zeip'];
    const usualSuspectsToAdd = PotentialParticipants.filter(c =>
        usualSuspects.some(suspect => c.value.includes(suspect)) && !participantIds.includes(c.key)
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
                        <div key={p.appUserId} className="participant-card">
                            <div className="participant-avatar">
                                {getInitials(p.appUser.displayName)}
                            </div>
                            <span className="participant-name">{p.appUser.displayName}</span>
                            <Button
                                size='mini'
                                icon='close'
                                onClick={() => handleRemoveParticipant(p.appUserId)}
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
            {PotentialParticipants.filter(c => !participantIds.includes(c.key)).length > 0 && (
                <>
                    <h4 style={{ marginBottom: 'var(--spacing-sm)' }}>Lisää osallistuja</h4>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--spacing-sm)' }}>
                        {PotentialParticipants
                            .filter(c => !participantIds.includes(c.key))
                            .map(user => (
                                <Button
                                    key={user.key}
                                    size='tiny'
                                    className='btn-secondary'
                                    onClick={() => handleAddParticipant(user.key)}
                                    loading={loading}
                                    disabled={loading}
                                >
                                    <Icon name='plus' /> {user.value}
                                </Button>
                            ))}
                    </div>
                </>
            )}
        </div>
    );
});
