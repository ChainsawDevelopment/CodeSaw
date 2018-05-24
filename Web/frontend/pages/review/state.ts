import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';

export interface RevisionRange {
    previous: number;
    current: number;
}

export interface ReviewState {
    availableRevisions: number[];
    range: RevisionRange;
}

const createAction = actionCreatorFactory('REVIEW');

export const selectPreviousVersion = createAction<{ revision: number }>('SELECT_PREVIOUS');
export const selectCurrentVersion = createAction<{ revision: number }>('SELECT_CURRENT');

const initial: ReviewState = {
    availableRevisions: [1,2,3,4,5,6,7],
    range: {
        previous: 2,
        current: 4
    }
};

export const reviewReducer = (state: ReviewState = initial, action: AnyAction) => {
    if(isType(action, selectPreviousVersion)) {
        return {
            ...state,
            range: {
                ...state.range,
                previous: action.payload.revision
            }
        };
    }

    if(isType(action, selectCurrentVersion)) {
        return {
            ...state,
            range: {
                ...state.range,
                current: action.payload.revision
            }
        };
    }

    return state;
} 