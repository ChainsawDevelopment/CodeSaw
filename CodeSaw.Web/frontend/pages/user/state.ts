import { UserState } from '../../rootState';
import actionCreatorFactory, { AnyAction } from 'typescript-fsa';

const initial: UserState = {
    username: '',
    name: '',
    avatarUrl: '',
};

const createAction = actionCreatorFactory('USER');
export const loadCurrentUser = createAction('LOAD_CURRENT_USER');
export const currentUserLoaded = createAction<{ username: string; name: string; avatarUrl: string }>(
    'CURRENT_USER_LOADED',
);

export const usersReducer = (state: UserState = initial, action: AnyAction): UserState => {
    if (currentUserLoaded.match(action)) {
        return {
            ...state,
            ...action.payload,
        };
    }

    return state;
};
