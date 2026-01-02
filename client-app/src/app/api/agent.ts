import axios, { AxiosError, AxiosResponse } from "axios";
import { Invoice, ExpenseItem, ExpenseTypeOption, Creditor } from "Invoices";
import { toast } from 'react-toastify';

const sleep = (delay: number) => {
    return new Promise((resolve) => {
        setTimeout(resolve, delay)
    })
}

axios.defaults.baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

axios.interceptors.response.use(async (response: AxiosResponse) => {
    try {
        await sleep(1000);
        return response;
    } catch (error) {
        console.log(error);
        return await Promise.reject(error);
    }
}, (error: AxiosError) => {
    const { data, status } = error.response as AxiosResponse;

    switch (status) {
        case 400:
            toast.error('Bad request');
            break;
        case 401:
            toast.error('Unauthorized');
            break;
        case 404:
            toast.error('Not found');
            break;
        case 500:
            toast.error('Server error');
            break;
        default:
            toast.error('An error occurred');
    }

    return Promise.reject(error);
})

const responseBody = <T>(response: AxiosResponse<T>) => response.data;

const requests = {
    get: <T>(url: string) => axios.get<T>(url).then(responseBody),
    post: <T>(url: string, body: {}) => axios.post<T>(url, body).then(responseBody),
    put: <T>(url: string, body: {}) => axios.put<T>(url, body).then(responseBody),
    del: <T>(url: string) => axios.delete<T>(url).then(responseBody)
}

const Invoices = {
    list: () => requests.get<Invoice[]>('/invoices'),
    details: (id: string) => requests.get<Invoice>(`/invoices/${id}`),
    create: (invoice: Invoice) => requests.post<void>('/invoices', invoice),
    update: (invoice: Invoice) => requests.put<void>(`/invoices/${invoice.id}`, invoice),
    delete: (id: string) => requests.del<void>(`/invoices/${id}`),
    addParticipant: (invoiceId: string, creditorId: number) => requests.post<void>(`/invoices/${invoiceId}/participants/${creditorId}`, {}),
    removeParticipant: (invoiceId: string, creditorId: number) => requests.del<void>(`/invoices/${invoiceId}/participants/${creditorId}`)
}

const ExpenseItems = {
    list: () => requests.get<ExpenseItem[]>('/expenseitems'),
    details: (id: string) => requests.get<ExpenseItem>(`/expenseitems/${id}`),
    create: (expenseItem: ExpenseItem) => requests.post<void>('/expenseitems', expenseItem),
    createForInvoice: (invoiceId: string, expenseItem: ExpenseItem) => requests.post<void>(`/expenseitems/${invoiceId}`, expenseItem),
    update: (expenseItem: ExpenseItem) => requests.put<void>(`/expenseitems/${expenseItem.id}`, expenseItem),
    delete: (id: string) => requests.del<void>(`/expenseitems/${id}`),
    addPayer: (expenseItemId: string, creditorId: number) => requests.post<void>(`/expenseitems/${expenseItemId}/payers/${creditorId}`, {}),
    removePayer: (expenseItemId: string, creditorId: number) => requests.del<void>(`/expenseitems/${expenseItemId}/payers/${creditorId}`)
}

const ExpenseTypes = {
    list: () => requests.get<ExpenseTypeOption[]>('/expensetypes')
}

const Creditors = {
    list: () => requests.get<Creditor[]>('/creditors'),
    create: (creditor: Creditor) => requests.post<void>('/creditors', creditor),
    update: (creditor: Creditor) => requests.put<void>(`/creditors/${creditor.id}`, creditor),
    delete: (id: number) => requests.del<void>(`/creditors/${id}`)
}

const agent = {
    Invoices,
    ExpenseItems,
    ExpenseTypes,
    Creditors
}

export default agent;