import { observer } from "mobx-react-lite";
import { useEffect, useState } from "react";
import { Button, Form, Icon, Modal, Table } from "semantic-ui-react";
import agent, { CreateUser, UpdateUser, UserManagement } from "../../app/api/agent";
import { toast } from "react-toastify";

export default observer(function AdminPage() {
    const [users, setUsers] = useState<UserManagement[]>([]);
    const [loading, setLoading] = useState(false);
    const [editingUser, setEditingUser] = useState<UserManagement | null>(null);
    const [createModalOpen, setCreateModalOpen] = useState(false);
    const [editModalOpen, setEditModalOpen] = useState(false);
    const [deleteModalOpen, setDeleteModalOpen] = useState(false);
    const [userToDelete, setUserToDelete] = useState<UserManagement | null>(null);

    const [createForm, setCreateForm] = useState<CreateUser>({
        userName: '',
        displayName: '',
        email: ''
    });

    const [editForm, setEditForm] = useState<UpdateUser>({
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
        return editForm.phoneNumber !== undefined && editForm.phoneNumber.trim() !== '';
    };

    // Check if bank account is valid
    const hasBankAccount = (): boolean => {
        return editForm.bankAccount !== undefined && isValidIBAN(editForm.bankAccount);
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
        loadUsers();
    }, []);

    // Clear preferred payment method if it becomes invalid
    useEffect(() => {
        if (editForm.preferredPaymentMethod === 'Pankki' && !hasBankAccount()) {
            setEditForm({ ...editForm, preferredPaymentMethod: '' });
        }
        if (editForm.preferredPaymentMethod === 'MobilePay' && !hasPhoneNumber()) {
            setEditForm({ ...editForm, preferredPaymentMethod: '' });
        }
    }, [editForm.bankAccount, editForm.phoneNumber]);

    const loadUsers = async () => {
        setLoading(true);
        try {
            const data = await agent.Admin.listUsers();
            setUsers(data);
        } catch (error) {
            console.error(error);
            toast.error('Käyttäjien lataus epäonnistui');
        } finally {
            setLoading(false);
        }
    };

    const handleCreateUser = async () => {
        try {
            const newUser = await agent.Admin.createUser(createForm);
            setUsers([...users, newUser]);
            setCreateModalOpen(false);
            setCreateForm({ userName: '', displayName: '', email: '' });
            toast.success('Käyttäjä luotu onnistuneesti. Väliaikainen salasana: TempPass123!');
        } catch (error: any) {
            toast.error(error.response?.data || 'Käyttäjän luonti epäonnistui');
        }
    };

    const handleUpdateUser = async () => {
        if (!editingUser) return;
        try {
            await agent.Admin.updateUser(editingUser.id, editForm);
            setUsers(users.map(u => u.id === editingUser.id ? { ...u, ...editForm } : u));
            setEditModalOpen(false);
            setEditingUser(null);
            toast.success('Käyttäjä päivitetty onnistuneesti');
        } catch (error: any) {
            toast.error(error.response?.data || 'Käyttäjän päivitys epäonnistui');
        }
    };

    const handleSendPasswordReset = async (userId: string, userEmail: string) => {
        try {
            const response = await agent.Admin.sendPasswordResetLink(userId);
            toast.success(response.message);
            await loadUsers();
        } catch (error) {
            toast.error('Salasanan palautuslinkin lähetys epäonnistui');
        }
    };

    const openEditModal = (user: UserManagement) => {
        setEditingUser(user);
        setEditForm({
            displayName: user.displayName,
            email: user.email,
            phoneNumber: user.phoneNumber || '',
            bankAccount: user.bankAccount || '',
            preferredPaymentMethod: user.preferredPaymentMethod || ''
        });
        setEditModalOpen(true);
    };

    const openDeleteModal = (user: UserManagement) => {
        setUserToDelete(user);
        setDeleteModalOpen(true);
    };

    const handleDeleteUser = async () => {
        if (!userToDelete) return;
        try {
            await agent.Admin.deleteUser(userToDelete.id);
            setUsers(users.filter(u => u.id !== userToDelete.id));
            setDeleteModalOpen(false);
            setUserToDelete(null);
            toast.success('Käyttäjä poistettu onnistuneesti');
        } catch (error: any) {
            toast.error(error.response?.data || 'Käyttäjän poisto epäonnistui');
        }
    };

    const handleToggleAdminRole = async (userId: string, isCurrentlyAdmin: boolean) => {
        try {
            if (isCurrentlyAdmin) {
                await agent.Admin.revokeAdminRole(userId);
                toast.success('Admin-oikeudet poistettu');
            } else {
                await agent.Admin.grantAdminRole(userId);
                toast.success('Admin-oikeudet myönnetty');
            }
            await loadUsers();
        } catch (error: any) {
            toast.error(error.response?.data || 'Admin-oikeuksien muutos epäonnistui');
        }
    };

    return (
        <div className="animate-fade-in">
            <h1 style={{ marginBottom: 'var(--spacing-xl)' }}>
                <Icon name="users" /> Käyttäjähallinta
            </h1>

            <div className="glass-card" style={{ padding: 'var(--spacing-lg)' }}>
                <div style={{ marginBottom: 'var(--spacing-lg)' }}>
                    <Button className="btn-primary" onClick={() => setCreateModalOpen(true)}>
                        <Icon name='plus' /> Lisää käyttäjä
                    </Button>
                </div>

                <div style={{ overflowX: 'auto' }}>
                    <Table celled>
                        <Table.Header>
                            <Table.Row>
                                <Table.HeaderCell>Käyttäjänimi</Table.HeaderCell>
                                <Table.HeaderCell>Näyttönimi</Table.HeaderCell>
                                <Table.HeaderCell>Sähköposti</Table.HeaderCell>
                                <Table.HeaderCell>Admin</Table.HeaderCell>
                                <Table.HeaderCell>Salasana vaihdettava</Table.HeaderCell>
                                <Table.HeaderCell>Toiminnot</Table.HeaderCell>
                            </Table.Row>
                        </Table.Header>

                        <Table.Body>
                            {users.map(user => (
                                <Table.Row key={user.id}>
                                    <Table.Cell>{user.userName}</Table.Cell>
                                    <Table.Cell>{user.displayName}</Table.Cell>
                                    <Table.Cell>{user.email}</Table.Cell>
                                    <Table.Cell>
                                        <Button
                                            size='small'
                                            toggle
                                            active={user.isAdmin}
                                            onClick={() => handleToggleAdminRole(user.id, user.isAdmin)}
                                            className={user.isAdmin ? "btn-success" : "btn-secondary"}
                                        >
                                            {user.isAdmin ? 'Kyllä' : 'Ei'}
                                        </Button>
                                    </Table.Cell>
                                    <Table.Cell>{user.mustChangePassword ? 'Kyllä' : 'Ei'}</Table.Cell>
                                    <Table.Cell>
                                        <div style={{ display: 'flex', gap: 'var(--spacing-sm)', flexWrap: 'wrap' }}>
                                            <Button size='small' className="btn-secondary" onClick={() => openEditModal(user)}>
                                                <Icon name='edit' /> Muokkaa
                                            </Button>
                                            <Button
                                                size='small'
                                                className="btn-primary"
                                                onClick={() => handleSendPasswordReset(user.id, user.email)}
                                            >
                                                <Icon name='key' /> Nollaa salasana
                                            </Button>
                                            <Button
                                                size='small'
                                                color='red'
                                                onClick={() => openDeleteModal(user)}
                                            >
                                                <Icon name='trash' /> Poista
                                            </Button>
                                        </div>
                                    </Table.Cell>
                                </Table.Row>
                            ))}
                        </Table.Body>
                    </Table>
                </div>
            </div>

            {/* Create User Modal */}
            <Modal open={createModalOpen} onClose={() => setCreateModalOpen(false)}>
                <Modal.Header>Luo uusi käyttäjä</Modal.Header>
                <Modal.Content>
                    <Form>
                        <Form.Input
                            label='Käyttäjänimi'
                            value={createForm.userName}
                            onChange={(e) => setCreateForm({ ...createForm, userName: e.target.value })}
                        />
                        <Form.Input
                            label='Näyttönimi'
                            value={createForm.displayName}
                            onChange={(e) => setCreateForm({ ...createForm, displayName: e.target.value })}
                        />
                        <Form.Input
                            label='Sähköposti'
                            type='email'
                            value={createForm.email}
                            onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                        />
                    </Form>
                    <p style={{ marginTop: 'var(--spacing-md)', color: 'var(--text-secondary)' }}>
                        Käyttäjälle asetetaan väliaikainen salasana (TempPass123!), joka on vaihdettava ensimmäisellä kirjautumisella.
                    </p>
                </Modal.Content>
                <Modal.Actions>
                    <Button className="btn-secondary" onClick={() => setCreateModalOpen(false)}>Peruuta</Button>
                    <Button className="btn-primary" onClick={handleCreateUser}>Luo käyttäjä</Button>
                </Modal.Actions>
            </Modal>

            {/* Edit User Modal */}
            <Modal open={editModalOpen} onClose={() => setEditModalOpen(false)}>
                <Modal.Header>Muokkaa käyttäjää</Modal.Header>
                <Modal.Content>
                    <Form>
                        <Form.Input
                            label='Näyttönimi'
                            value={editForm.displayName}
                            onChange={(e) => setEditForm({ ...editForm, displayName: e.target.value })}
                        />
                        <Form.Input
                            label='Sähköposti'
                            type='email'
                            value={editForm.email}
                            onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                        />
                        <Form.Input
                            label='Puhelinnumero'
                            value={editForm.phoneNumber}
                            onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })}
                            placeholder='Esim. +358401234567'
                        />
                        <Form.Input
                            label='Pankkitili (IBAN)'
                            value={editForm.bankAccount}
                            onChange={(e) => setEditForm({ ...editForm, bankAccount: e.target.value })}
                            placeholder='Esim. FI12 3456 7890 1234 56'
                        />
                        <Form.Select
                            label='Ensisijainen maksutapa'
                            value={editForm.preferredPaymentMethod}
                            options={getPaymentMethodOptions()}
                            onChange={(e, { value }) => setEditForm({ ...editForm, preferredPaymentMethod: value as string })}
                            placeholder='Valitse maksutapa'
                        />
                    </Form>
                </Modal.Content>
                <Modal.Actions>
                    <Button className="btn-secondary" onClick={() => setEditModalOpen(false)}>Peruuta</Button>
                    <Button className="btn-primary" onClick={handleUpdateUser}>Tallenna</Button>
                </Modal.Actions>
            </Modal>

            {/* Delete User Confirmation Modal */}
            <Modal open={deleteModalOpen} onClose={() => setDeleteModalOpen(false)} size='small'>
                <Modal.Header>Poista käyttäjä</Modal.Header>
                <Modal.Content>
                    <p>Haluatko varmasti poistaa käyttäjän <strong>{userToDelete?.displayName}</strong> ({userToDelete?.userName})?</p>
                    <p style={{ color: 'var(--text-secondary)', marginTop: 'var(--spacing-md)' }}>
                        Tämä toiminto ei ole palautettavissa.
                    </p>
                </Modal.Content>
                <Modal.Actions>
                    <Button className="btn-secondary" onClick={() => setDeleteModalOpen(false)}>Peruuta</Button>
                    <Button color='red' onClick={handleDeleteUser}>
                        <Icon name='trash' /> Poista käyttäjä
                    </Button>
                </Modal.Actions>
            </Modal>
        </div>
    );
});
