import React from 'react'
import { Button, Container, Menu } from 'semantic-ui-react'
import { useStore } from '../stores/store'


export default function NavBar()
{

    const {invoiceStore} = useStore();
    return (
        <Menu inverted fixed='top'>
            <Container>
                <Menu.Item header>
                    <img src="/assets/logo.png" alt = "logo" style={{marginRight: '10px'}}/>
                    Mökkilan Invoices
                </Menu.Item>
                {/* <Menu.Item name='Invoices'></Menu.Item> */}
                <Menu.Item>
                    <Button onClick={() => invoiceStore.openForm()} positive content="Create Invoice"></Button>
                </Menu.Item>
            </Container>
        </Menu>
    )
}