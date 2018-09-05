import { AnyAction } from "../../../node_modules/redux";
import actionCreatorFactory from "typescript-fsa";

export interface LoadingState {
    inProgressOperationsCount: number;
}

const init: LoadingState = {
    inProgressOperationsCount: 0
};

const createAction = actionCreatorFactory('LOADING');

export const startOperationAction = createAction<{}>('START_OPERATION');

export const stopOperationAction = createAction<{}>('STOP_OPERATION');

export const loadingReducer = (state: LoadingState = init, action: AnyAction) => {
    if (startOperationAction.match(action)) {
        return {
            ...state,
            inProgressOperationsCount: state.inProgressOperationsCount + 1
        };
    }

    if (stopOperationAction.match(action)) {
        return {
            ...state,
            inProgressOperationsCount: state.inProgressOperationsCount - 1
        };
    }

    return state;
}