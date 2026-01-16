import React from 'react'
import { Button, Container, Dropdown, Menu } from 'semantic-ui-react'
import { useStore } from '../stores/store'
import { observer } from 'mobx-react-lite'
import { Link, NavLink } from 'react-router-dom'


export default observer(function NavBar() {

    const { userStore } = useStore();
    return (
        <Menu inverted fixed='top'>
            <Container>
                <Menu.Item as={NavLink} to='/' header>
                    <img src="/assets/logo.png" alt="logo" style={{ marginRight: '10px' }} />
                    Mökkilan Invoices
                </Menu.Item>
                {userStore.isLoggedIn && (
                    <>
                        <Menu.Item as={NavLink} to='/invoices' name='Invoices' />
                        <Menu.Item as={NavLink} to='/scan-receipt' name='Scan Receipt' />
                        <Menu.Item>
                            <Button as={NavLink} to='/createInvoice' positive content="Create Invoice" />
                        </Menu.Item>
                        {userStore.user?.roles?.includes('Admin') && (
                            <Menu.Item as={NavLink} to='/admin' name='Admin' />
                        )}
                        <Menu.Item position='right'>
                            <Dropdown pointing='top left' text={userStore.user?.displayName}>
                                <Dropdown.Menu>
                                    <Dropdown.Item as={Link} to='/changePassword' text='Change Password' icon='key' />
                                    <Dropdown.Item onClick={userStore.logout} text='Logout' icon='power' />
                                </Dropdown.Menu>
                            </Dropdown>
                        </Menu.Item>
                    </>
                )}
                {!userStore.isLoggedIn && (
                    <Menu.Item position='right'>
                        <Button as={Link} to='/login' content='Login' />
                    </Menu.Item>
                )}
            </Container>
        </Menu>
    )
})