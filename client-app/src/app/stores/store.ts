import { createContext, useContext } from "react";
import InvoiceStore from "./invoiceStore";
import UserStore from "./userStore";

interface Store {
    invoiceStore: InvoiceStore;
    userStore: UserStore;
}

export const store: Store = {
    invoiceStore: new InvoiceStore(),
    userStore: new UserStore()
}

export const StoreContext = createContext(store);

export function useStore() {
    return useContext(StoreContext);
}