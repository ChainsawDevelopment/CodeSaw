import { AnyAction } from '../../../node_modules/redux';
import actionCreatorFactory from 'typescript-fsa';

export interface LoadingState {
    inProgressOperationsCount: number;
    inProgressOperationLastMessage: string;
}

const init: LoadingState = {
    inProgressOperationsCount: 0,
    inProgressOperationLastMessage: '',
};

const createAction = actionCreatorFactory('LOADING');

export const startOperationAction = createAction<{ message?: string }>('START_OPERATION');

export const setOperationMessageAction = createAction<{ message?: string }>('SET_OPERATION_MESSAGE');

export const stopOperationAction = createAction<{}>('STOP_OPERATION');

export const loadingReducer = (
    state: LoadingState = init,
    action: AnyAction,
): { inProgressOperationLastMessage: string; inProgressOperationsCount: number } => {
    if (startOperationAction.match(action)) {
        return {
            ...state,
            inProgressOperationsCount: state.inProgressOperationsCount + 1,
            inProgressOperationLastMessage: action.payload.message,
        };
    }

    if (stopOperationAction.match(action)) {
        return {
            ...state,
            inProgressOperationsCount: state.inProgressOperationsCount - 1,
            inProgressOperationLastMessage: '',
        };
    }

    if (setOperationMessageAction.match(action)) {
        return {
            ...state,
            inProgressOperationLastMessage: action.payload.message,
        };
    }

    return state;
};
