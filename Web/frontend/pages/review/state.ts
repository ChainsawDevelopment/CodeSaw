import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';
import { RevisionRangeInfo, FileDiff, ReviewInfo } from '../../api/reviewer';

export type RevisionId = 'base' | number | 'provisional';

export interface RevisionRange {
    previous: RevisionId;
    current: RevisionId;
}

export interface ReviewState {
    range: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: string;
    selectedFileDiff: FileDiff;
    currentReview: ReviewInfo;
}

const createAction = actionCreatorFactory('REVIEW');

export interface SelectCurrentRevisions {
    range: RevisionRange;
}
export const selectCurrentRevisions = createAction<SelectCurrentRevisions>('SELECT_CURRENT_REVISIONS');
export const loadedRevisionsRangeInfo = createAction<RevisionRangeInfo>('LOADED_REVISION_RANGE_INFO');

export const selectFileForView = createAction<{ path: string }>('SELECT_FILE_FOR_VIEW');

export const loadedFileDiff = createAction<FileDiff>('LOADED_FILE_DIFF');

export const loadReviewInfo = createAction<{ projectId: number; reviewId: number }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<ReviewInfo>('LOADED_REVIEW_INFO');

const initial: ReviewState = {
    range: {
        previous: 2,
        current: 4
    },
    rangeInfo: null,
    selectedFile: null,
    selectedFileDiff: null,
    currentReview: {
        hasProvisionalRevision: false,
        pastRevisions: [],
        projectId: 0,
        reviewId: 0,
        title: ''
    }
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

    if (loadedFileDiff.match(action)) {
        return {
            ...state,
            selectedFileDiff: action.payload
        };
    }

    if (loadedReviewInfo.match(action)) {
        return {
            ...state,
            currentReview: action.payload
        };
    }

    return state;
} 