import { makeAutoObservable, runInAction } from "mobx";
import agent, { User, UserFormValues } from "../api/agent";
import { router } from "../router/Routes";

export default class UserStore {
    user: User | null = null;

    constructor() {
        makeAutoObservable(this)
    }

    get isLoggedIn() {
        return !!this.user;
    }

    login = async (creds: UserFormValues) => {
        try {
            const user = await agent.Account.login(creds);
            runInAction(() => {
                this.user = user;
            });
            window.localStorage.setItem('jwt', user.token);
            router.navigate('/invoices');
        } catch (error) {
            throw error;
        }
    }

    register = async (creds: UserFormValues) => {
        try {
            const user = await agent.Account.register(creds);
            runInAction(() => {
                this.user = user;
            });
            window.localStorage.setItem('jwt', user.token);
            router.navigate('/invoices');
        } catch (error) {
            throw error;
        }
    }

    logout = () => {
        this.user = null;
        window.localStorage.removeItem('jwt');
        router.navigate('/');
    }

    getUser = async () => {
        try {
            const user = await agent.Account.current();
            runInAction(() => {
                this.user = user;
            });
        } catch (error) {
            console.log(error);
        }
    }
}
