import React, { useEffect, useState } from 'react';
import { Container } from 'semantic-ui-react';
import { Invoice } from 'Invoices';
import NavBar from './NavBar';
import InvoiceDashboard from '../../features/activities/dashboard/InvoiceDashboard';
import { v4 as uuid } from 'uuid';
import agent from '../api/agent';
import LoadingComponent from './LoadingComponent';


function App() {

  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | undefined>(undefined);
  const [editMode, setEditMode] = useState(false);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    agent.Invoices.list().then(response => {
      setInvoices(response)
      setLoading(false);
    })
  }, []
  )


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
    setSubmitting(true);
    if (invoice.id) {
      agent.Invoices.update(invoice).then(() => {
        setInvoices([...invoices.filter(x => x.id !== invoice.id), invoice])
        setSelectedInvoice(invoice);
        setEditMode(false);
        setSubmitting(false);
      })
    } else {
      invoice.id = uuid();
      agent.Invoices.create(invoice).then(() => {
        setInvoices([...invoices, invoice])
        setSelectedInvoice(invoice);
        setEditMode(false);
        setSubmitting(false);
      })
    }

  }

  function handleDeleteInvoice(id: string) {
    setSubmitting(true);
    agent.Invoices.delete(id).then(() => {
      setInvoices([...invoices.filter(x => x.id !== id)]);
      setSubmitting(false)
    })
  }


  if (loading) return <LoadingComponent content='Loading app' />

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
          deleteInvoice={handleDeleteInvoice}
          submitting={submitting} />
      </Container>
    </>
  );
}

export default App;
