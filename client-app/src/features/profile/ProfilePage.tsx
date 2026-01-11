import { observer } from "mobx-react-lite";
import { useEffect, useState } from "react";
import { Button, Form, Icon } from "semantic-ui-react";
import agent, { UpdateProfile, UserProfile } from "../../app/api/agent";
import { toast } from "react-toastify";
import LoadingComponent from "../../app/layout/LoadingComponent";

export default observer(function ProfilePage() {
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [loading, setLoading] = useState(false);
    const [loadingInitial, setLoadingInitial] = useState(true);
    const [editMode, setEditMode] = useState(false);

    const [formData, setFormData] = useState<UpdateProfile>({
        displayName: '',
        email: '',
        phoneNumber: '',
        bankAccount: ''
    });

    useEffect(() => {
        loadProfile();
    }, []);

    const loadProfile = async () => {
        setLoadingInitial(true);
        try {
            const data = await agent.Account.getProfile();
            setProfile(data);
            setFormData({
                displayName: data.displayName,
                email: data.email,
                phoneNumber: data.phoneNumber || '',
                bankAccount: data.bankAccount || ''
            });
        } catch (error) {
            console.error(error);
            toast.error('Profiilin lataus epäonnistui');
        } finally {
            setLoadingInitial(false);
        }
    };

    const handleSubmit = async () => {
        setLoading(true);
        try {
            await agent.Account.updateProfile(formData);
            await loadProfile();
            setEditMode(false);
            toast.success('Profiili päivitetty onnistuneesti');
        } catch (error: any) {
            toast.error(error.response?.data || 'Profiilin päivitys epäonnistui');
        } finally {
            setLoading(false);
        }
    };

    const handleCancel = () => {
        if (profile) {
            setFormData({
                displayName: profile.displayName,
                email: profile.email,
                phoneNumber: profile.phoneNumber || '',
                bankAccount: profile.bankAccount || ''
            });
        }
        setEditMode(false);
    };

    if (loadingInitial || !profile) return <LoadingComponent content="Ladataan profiilia..." />;

    return (
        <div className="animate-fade-in">
            <h1 style={{ marginBottom: 'var(--spacing-xl)' }}>
                <Icon name="user" /> Profiili
            </h1>

            <div className="glass-card" style={{ padding: 'var(--spacing-lg)', maxWidth: '600px' }}>
                {!editMode ? (
                    <>
                        <div style={{ marginBottom: 'var(--spacing-lg)' }}>
                            <div style={{ marginBottom: 'var(--spacing-md)' }}>
                                <strong>Käyttäjänimi:</strong>
                                <div>{profile.userName}</div>
                            </div>
                            <div style={{ marginBottom: 'var(--spacing-md)' }}>
                                <strong>Näyttönimi:</strong>
                                <div>{profile.displayName}</div>
                            </div>
                            <div style={{ marginBottom: 'var(--spacing-md)' }}>
                                <strong>Sähköposti:</strong>
                                <div>{profile.email}</div>
                            </div>
                            <div style={{ marginBottom: 'var(--spacing-md)' }}>
                                <strong>Puhelinnumero:</strong>
                                <div>{profile.phoneNumber || '-'}</div>
                            </div>
                            <div style={{ marginBottom: 'var(--spacing-md)' }}>
                                <strong>Pankkitili:</strong>
                                <div>{profile.bankAccount || '-'}</div>
                            </div>
                        </div>
                        <Button className="btn-primary" onClick={() => setEditMode(true)}>
                            <Icon name="edit" /> Muokkaa profiilia
                        </Button>
                    </>
                ) : (
                    <Form onSubmit={handleSubmit}>
                        <Form.Input
                            label='Näyttönimi'
                            value={formData.displayName}
                            onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
                            required
                        />
                        <Form.Input
                            label='Sähköposti'
                            type='email'
                            value={formData.email}
                            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                            required
                        />
                        <Form.Input
                            label='Puhelinnumero'
                            value={formData.phoneNumber}
                            onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                            placeholder='Esim. +358401234567'
                        />
                        <Form.Input
                            label='Pankkitili (IBAN)'
                            value={formData.bankAccount}
                            onChange={(e) => setFormData({ ...formData, bankAccount: e.target.value })}
                            placeholder='Esim. FI12 3456 7890 1234 56'
                        />
                        <div style={{ display: 'flex', gap: 'var(--spacing-md)', marginTop: 'var(--spacing-lg)' }}>
                            <Button type='submit' className="btn-primary" loading={loading} disabled={loading}>
                                <Icon name="save" /> Tallenna
                            </Button>
                            <Button type='button' className="btn-secondary" onClick={handleCancel} disabled={loading}>
                                Peruuta
                            </Button>
                        </div>
                    </Form>
                )}
            </div>
        </div>
    );
});
