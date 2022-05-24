import axios, { AxiosResponse } from "axios";
import { request } from "http";
import { Invoice } from "Invoices";
import { resolve } from "path";

const sleep = (delay: number) => {
    return new Promise((resolve) => {
        setTimeout(resolve, delay)
    })
}

axios.defaults.baseURL = 'http://localhost:5000/api';
axios.interceptors.response.use(async response => {
    try {
        await sleep(1000);
        return response;
    } catch (error) {
        console.log(error);
        return await Promise.reject(error);
    }
})

const responseBody = <T> (response: AxiosResponse<T>) => response.data;

const requests = {
    get: <T>(url: string) => axios.get<T>(url).then(responseBody),
    post: <T>(url: string, body:{}) => axios.post<T>(url, body).then(responseBody),
    put: <T>(url: string, body: {}) => axios.put<T>(url, body).then(responseBody),
    del: <T>(url: string) => axios.delete<T>(url).then(responseBody)
}

const Invoices = {
    list: () => requests.get<Invoice[]>('/invoices'),
    details:(id:string) => requests.get<Invoice>(`/invoices>/${id}`),
    create: (invoice: Invoice) => axios.post<void>('/invoices', invoice),
    update: (invoice: Invoice) => axios.put<void>(`/invoices/${invoice.id}`, invoice),
    delete: (id:string) => axios.delete(`/invoices/${id}`)

}

const agent = {
    Invoices
}

export default agent;