import { createBrowserRouter, Navigate, RouteObject } from "react-router-dom";
import App from "../layout/App";
import InvoiceDashboard from "../../features/invoices/dashboard/InvoiceDashboard";
import InvoiceDetails from "../../features/invoices/details/InvoiceDetails";
import InvoiceForm from "../../features/invoices/form/InvoiceForm";
import LoginForm from "../../features/users/LoginForm";
import ChangePasswordForm from "../../features/users/ChangePasswordForm";

export const routes: RouteObject[] = [
    {
        path: '/',
        element: <App />,
        children: [
            { path: 'invoices', element: <InvoiceDashboard /> },
            { path: 'invoices/:id', element: <InvoiceDetails /> },
            { path: 'createInvoice', element: <InvoiceForm key='create' /> },
            { path: 'manage/:id', element: <InvoiceForm key='manage' /> },
            { path: 'login', element: <LoginForm /> },
            { path: 'changePassword', element: <ChangePasswordForm /> },
            { path: '*', element: <Navigate replace to='/invoices' /> },
        ]
    }
]

export const router = createBrowserRouter(routes);
