import { UserState } from "../../rootState";
import actionCreatorFactory, { AnyAction } from "typescript-fsa";

const initial: UserState = {
    userName: "",
    givenName: ""
}

const createAction = actionCreatorFactory('USER');
export const loadCurrentUser = createAction('LOAD_CURRENT_USER');
export const currentUserLoaded = createAction<{userName: string; givenName: string}>('CURRENT_USER_LOADED');

export const usersReducer = (state: UserState = initial, action: AnyAction): UserState => {
    if (currentUserLoaded.match(action)) {
        return {
            ...state,
            ...action.payload
        }
    }

    return state;
}