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
        bankAccount: '',
        preferredPaymentMethod: ''
    });

    // Validate IBAN format (basic validation)
    const isValidIBAN = (iban: string): boolean => {
        if (!iban) return false;
        const cleanIban = iban.replace(/\s/g, '');
        return /^[A-Z]{2}\d{2}[A-Z0-9]+$/.test(cleanIban) && cleanIban.length >= 15;
    };

    // Check if phone number exists
    const hasPhoneNumber = (): boolean => {
        return formData.phoneNumber !== undefined && formData.phoneNumber.trim() !== '';
    };

    // Check if bank account is valid
    const hasBankAccount = (): boolean => {
        return formData.bankAccount !== undefined && isValidIBAN(formData.bankAccount);
    };

    // Get available payment method options
    const getPaymentMethodOptions = () => {
        const options = [
            { key: '', text: 'Ei valittu', value: '' }
        ];

        if (hasBankAccount()) {
            options.push({ key: 'Pankki', text: 'Pankki', value: 'Pankki' });
        }

        if (hasPhoneNumber()) {
            options.push({ key: 'MobilePay', text: 'MobilePay', value: 'MobilePay' });
        }

        return options;
    };

    useEffect(() => {
        loadProfile();
    }, []);

    // Clear preferred payment method if it becomes invalid
    useEffect(() => {
        if (formData.preferredPaymentMethod === 'Pankki' && !hasBankAccount()) {
            setFormData({ ...formData, preferredPaymentMethod: '' });
        }
        if (formData.preferredPaymentMethod === 'MobilePay' && !hasPhoneNumber()) {
            setFormData({ ...formData, preferredPaymentMethod: '' });
        }
    }, [formData.bankAccount, formData.phoneNumber]);

    const loadProfile = async () => {
        setLoadingInitial(true);
        try {
            const data = await agent.Account.getProfile();
            setProfile(data);
            setFormData({
                displayName: data.displayName,
                email: data.email,
                phoneNumber: data.phoneNumber || '',
                bankAccount: data.bankAccount || '',
                preferredPaymentMethod: data.preferredPaymentMethod || ''
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
                bankAccount: profile.bankAccount || '',
                preferredPaymentMethod: profile.preferredPaymentMethod || ''
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
                            <div style={{ marginBottom: 'var(--spacing-md)' }}>
                                <strong>Ensisijainen maksutapa:</strong>
                                <div>{profile.preferredPaymentMethod || '-'}</div>
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
                        <Form.Select
                            label='Ensisijainen maksutapa'
                            value={formData.preferredPaymentMethod}
                            options={getPaymentMethodOptions()}
                            onChange={(e, { value }) => setFormData({ ...formData, preferredPaymentMethod: value as string })}
                            placeholder='Valitse maksutapa'
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
