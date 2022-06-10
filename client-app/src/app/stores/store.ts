import { createContext, useContext } from "react";
import InvoiceStore from "./invoiceStore";

interface Store {
    invoiceStore : InvoiceStore
}

export const store: Store = {
    invoiceStore: new InvoiceStore()
}

export const StoreContext = createContext(store);

export function useStore() {
    return useContext(StoreContext);
}