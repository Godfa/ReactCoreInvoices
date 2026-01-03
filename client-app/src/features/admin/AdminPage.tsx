import { observer } from "mobx-react-lite";
import { useEffect, useState } from "react";
import { Button, Container, Form, Header, Icon, Modal, Segment, Table } from "semantic-ui-react";
import agent, { CreateUser, UpdateUser, UserManagement } from "../../app/api/agent";
import { toast } from "react-toastify";

export default observer(function AdminPage() {
    const [users, setUsers] = useState<UserManagement[]>([]);
    const [loading, setLoading] = useState(false);
    const [editingUser, setEditingUser] = useState<UserManagement | null>(null);
    const [createModalOpen, setCreateModalOpen] = useState(false);
    const [editModalOpen, setEditModalOpen] = useState(false);

    const [createForm, setCreateForm] = useState<CreateUser>({
        userName: '',
        displayName: '',
        email: ''
    });

    const [editForm, setEditForm] = useState<UpdateUser>({
        displayName: '',
        email: ''
    });

    useEffect(() => {
        loadUsers();
    }, []);

    const loadUsers = async () => {
        setLoading(true);
        try {
            const data = await agent.Admin.listUsers();
            setUsers(data);
        } catch (error) {
            console.error(error);
            toast.error('Failed to load users');
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
            toast.success('User created successfully. Temporary password: TempPass123!');
        } catch (error: any) {
            toast.error(error.response?.data || 'Failed to create user');
        }
    };

    const handleUpdateUser = async () => {
        if (!editingUser) return;
        try {
            await agent.Admin.updateUser(editingUser.id, editForm);
            setUsers(users.map(u => u.id === editingUser.id ? { ...u, ...editForm } : u));
            setEditModalOpen(false);
            setEditingUser(null);
            toast.success('User updated successfully');
        } catch (error: any) {
            toast.error(error.response?.data || 'Failed to update user');
        }
    };

    const handleSendPasswordReset = async (userId: string, userEmail: string) => {
        try {
            const response = await agent.Admin.sendPasswordResetLink(userId);
            toast.success(response.message);
            await loadUsers();
        } catch (error) {
            toast.error('Failed to send password reset link');
        }
    };

    const openEditModal = (user: UserManagement) => {
        setEditingUser(user);
        setEditForm({
            displayName: user.displayName,
            email: user.email
        });
        setEditModalOpen(true);
    };

    return (
        <Container>
            <Segment>
                <Header as='h2' color='teal'>
                    <Icon name='users' />
                    User Management
                </Header>
                <Button primary onClick={() => setCreateModalOpen(true)}>
                    <Icon name='plus' /> Add User
                </Button>

                <Table celled>
                    <Table.Header>
                        <Table.Row>
                            <Table.HeaderCell>Username</Table.HeaderCell>
                            <Table.HeaderCell>Display Name</Table.HeaderCell>
                            <Table.HeaderCell>Email</Table.HeaderCell>
                            <Table.HeaderCell>Must Change Password</Table.HeaderCell>
                            <Table.HeaderCell>Actions</Table.HeaderCell>
                        </Table.Row>
                    </Table.Header>

                    <Table.Body>
                        {users.map(user => (
                            <Table.Row key={user.id}>
                                <Table.Cell>{user.userName}</Table.Cell>
                                <Table.Cell>{user.displayName}</Table.Cell>
                                <Table.Cell>{user.email}</Table.Cell>
                                <Table.Cell>{user.mustChangePassword ? 'Yes' : 'No'}</Table.Cell>
                                <Table.Cell>
                                    <Button size='small' onClick={() => openEditModal(user)}>
                                        <Icon name='edit' /> Edit
                                    </Button>
                                    <Button
                                        size='small'
                                        color='orange'
                                        onClick={() => handleSendPasswordReset(user.id, user.email)}
                                    >
                                        <Icon name='key' /> Reset Password
                                    </Button>
                                </Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table>
            </Segment>

            {/* Create User Modal */}
            <Modal open={createModalOpen} onClose={() => setCreateModalOpen(false)}>
                <Modal.Header>Create New User</Modal.Header>
                <Modal.Content>
                    <Form>
                        <Form.Input
                            label='Username'
                            value={createForm.userName}
                            onChange={(e) => setCreateForm({ ...createForm, userName: e.target.value })}
                        />
                        <Form.Input
                            label='Display Name'
                            value={createForm.displayName}
                            onChange={(e) => setCreateForm({ ...createForm, displayName: e.target.value })}
                        />
                        <Form.Input
                            label='Email'
                            type='email'
                            value={createForm.email}
                            onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                        />
                    </Form>
                    <p style={{ marginTop: '1em', color: '#666' }}>
                        A temporary password (TempPass123!) will be assigned. User must change it on first login.
                    </p>
                </Modal.Content>
                <Modal.Actions>
                    <Button onClick={() => setCreateModalOpen(false)}>Cancel</Button>
                    <Button positive onClick={handleCreateUser}>Create</Button>
                </Modal.Actions>
            </Modal>

            {/* Edit User Modal */}
            <Modal open={editModalOpen} onClose={() => setEditModalOpen(false)}>
                <Modal.Header>Edit User</Modal.Header>
                <Modal.Content>
                    <Form>
                        <Form.Input
                            label='Display Name'
                            value={editForm.displayName}
                            onChange={(e) => setEditForm({ ...editForm, displayName: e.target.value })}
                        />
                        <Form.Input
                            label='Email'
                            type='email'
                            value={editForm.email}
                            onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                        />
                    </Form>
                </Modal.Content>
                <Modal.Actions>
                    <Button onClick={() => setEditModalOpen(false)}>Cancel</Button>
                    <Button positive onClick={handleUpdateUser}>Save</Button>
                </Modal.Actions>
            </Modal>
        </Container>
    );
});
