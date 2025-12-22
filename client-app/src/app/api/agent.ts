import axios, { AxiosError, AxiosResponse } from "axios";
import { Invoice, ExpenseItem, ExpenseTypeOption } from "Invoices";
import { toast } from 'react-toastify';

const sleep = (delay: number) => {
    return new Promise((resolve) => {
        setTimeout(resolve, delay)
    })
}

axios.defaults.baseURL = 'http://localhost:5000/api';

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
    delete: (id: string) => requests.del<void>(`/invoices/${id}`)
}

const ExpenseItems = {
    list: () => requests.get<ExpenseItem[]>('/expenseitems'),
    details: (id: string) => requests.get<ExpenseItem>(`/expenseitems/${id}`),
    create: (expenseItem: ExpenseItem) => requests.post<void>('/expenseitems', expenseItem),
    createForInvoice: (invoiceId: string, expenseItem: ExpenseItem) => requests.post<void>(`/expenseitems/${invoiceId}`, expenseItem),
    update: (expenseItem: ExpenseItem) => requests.put<void>(`/expenseitems/${expenseItem.id}`, expenseItem),
    delete: (id: string) => requests.del<void>(`/expenseitems/${id}`)
}

const ExpenseTypes = {
    list: () => requests.get<ExpenseTypeOption[]>('/expensetypes')
}

const agent = {
    Invoices,
    ExpenseItems,
    ExpenseTypes
}

export default agent;