import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';
import { RevisionRangeInfo } from '../../api/reviewer';

export interface RevisionRange {
    previous: number;
    current: number;
}

export interface ReviewState {
    availableRevisions: number[];
    range: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: string;
}

const createAction = actionCreatorFactory('REVIEW');

export interface SelectCurrentRevisions {
    range: RevisionRange;
}
export const selectCurrentRevisions = createAction<SelectCurrentRevisions>('SELECT_CURRENT_REVISIONS');
export const loadedRevisionsRangeInfo = createAction<RevisionRangeInfo>('LOADED_REVISION_RANGE_INFO');

export const selectFileForView = createAction<{ path: string }>('SELECT_FILE_FOR_VIEW');

const initial: ReviewState = {
    availableRevisions: [1, 2, 3, 4, 5, 6, 7],
    range: {
        previous: 2,
        current: 4
    },
    rangeInfo: null,
    selectedFile: null
};

export const reviewReducer = (state: ReviewState = initial, action: AnyAction): ReviewState => {
    if (isType(action, selectCurrentRevisions)) {
        return {
            ...state,
            range: action.payload.range
        };
    }

    if (loadedRevisionsRangeInfo.match(action)) {
        return {
            ...state,
            rangeInfo: action.payload
        }
    }

    if (selectFileForView.match(action)) {
        return {
            ...state,
            selectedFile: action.payload.path
        };
    }

    return state;
} 