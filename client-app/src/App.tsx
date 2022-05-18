import React, { useEffect, useState } from 'react';
import logo from './logo.svg';
import './App.css';
import axios from 'axios';
import { HeaderContent, ListContent, ListItem } from 'semantic-ui-react';


function App() {

  const [invoices, setInvoices] = useState([]);

  useEffect(() => {
      axios.get('http://localhost:5000/api/invoices').then(response => {
        console.log(response);  
        setInvoices(response.data);
      })
  }, [])

  return (
    <div>
      <HeaderContent as='h2' icon='users' content='Mökkilan invoices' />      
        <ListContent>
        {invoices.map((invoice: any) => (
            <ListItem key= {invoice.id}>
              {invoice.description}
            </ListItem>
          ))}
        </ListContent>     
    </div>
  );
}

export default App;
