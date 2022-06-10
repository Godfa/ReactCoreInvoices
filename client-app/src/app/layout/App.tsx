import React, { useEffect, useState } from 'react';
import { Button, Container } from 'semantic-ui-react';
import NavBar from './NavBar';
import InvoiceDashboard from '../../features/activities/dashboard/InvoiceDashboard';
import LoadingComponent from './LoadingComponent';
import { useStore } from '../stores/store';
import { observer } from 'mobx-react-lite';


function App() {

  const {invoiceStore}=useStore();

  useEffect(() => {
   invoiceStore.loadInvoices();
  }, [invoiceStore]
  )  

  if (invoiceStore.loadingInitial) return <LoadingComponent content='Loading app' />

  return (
    <>
      <NavBar  />
      <Container style={{ marginTop: '7em' }}>       
        <InvoiceDashboard />
      </Container>
    </>
  );
}

export default observer(App);
