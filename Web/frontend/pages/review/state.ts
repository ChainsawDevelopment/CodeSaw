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

export const selectCurrentRevisions = createAction<{ range: RevisionRange }>('SELECT_CURRENT_REVISIONS');

const initial: ReviewState = {
    availableRevisions: [1,2,3,4,5,6,7],
    range: {
        previous: 2,
        current: 4
    }
};

export const reviewReducer = (state: ReviewState = initial, action: AnyAction): ReviewState => {    
    if(isType(action, selectCurrentRevisions)) {
        return {
            ...state,
            range: action.payload.range
        };
    }

    return state;
} 