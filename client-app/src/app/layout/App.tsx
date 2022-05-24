import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Container } from 'semantic-ui-react';
import { Invoice } from 'Invoices';
import NavBar from './NavBar';
import InvoiceDashboard from '../../features/activities/dashboard/InvoiceDashboard';
import {v4 as uuid} from 'uuid';


function App() {

  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | undefined>(undefined);
  const [editMode, setEditMode] = useState(false);

  useEffect(() => {
    axios.get<Invoice[]>('http://localhost:5000/api/invoices').then(response => {
      console.log(response)
      setInvoices(response.data);
    })
  }, [])

  function handleSelectInvoice(id: string) {
    setSelectedInvoice(invoices.find(x => x.id === id))
  }

  function handleCancelSelectInvoice() {
    setSelectedInvoice(undefined);
  }

  function handleFormOpen(id?: string) {
    id ? handleSelectInvoice(id) : handleCancelSelectInvoice();
    setEditMode(true);
  }

  function handleFormClose() {
    setEditMode(false);
  }

  function handleCreateOrEditInvoice(invoice: Invoice) {
    invoice.id ? setInvoices([...invoices.filter(x => x.id !== invoice.id), invoice])
    : setInvoices([...invoices, {...invoice, id: uuid()}]);
    setEditMode(false);
    setSelectedInvoice(invoice);
  }

  function handleDeleteInvoice(id: string) {
    setInvoices([...invoices.filter(x => x.id !== id)]);
  }

  return (
    <>
      <NavBar openForm={handleFormOpen} />
      <Container style={{ marginTop: '7em' }}>
        <InvoiceDashboard
          invoices={invoices}
          selectedInvoice={selectedInvoice}
          selectInvoice={handleSelectInvoice}
          cancelSelectInvoice={handleCancelSelectInvoice}
          editMode={editMode}
          openForm={handleFormOpen}
          closeForm={handleFormClose}
          createOrEdit={handleCreateOrEditInvoice}
          deleteInvoice={handleDeleteInvoice} />
      </Container>
    </>
  );
}

export default App;
