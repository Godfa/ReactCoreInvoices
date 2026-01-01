import React from 'react';
import { createRoot } from 'react-dom/client';
import 'react-toastify/dist/ReactToastify.min.css';
import './app/layout/styles.css';
import App from './app/layout/App';
import reportWebVitals from './reportWebVitals';
import { store, StoreContext } from './app/stores/store';
import { ToastContainer } from 'react-toastify';

const container = document.getElementById('root');
const root = createRoot(container!);

root.render(
  <StoreContext.Provider value={store}>
    <ToastContainer position='bottom-right' hideProgressBar />
    <App />
  </StoreContext.Provider>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
