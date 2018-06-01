import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';
import { RevisionRangeInfo, FileDiff, ReviewInfo, RevisionRange, ReviewId } from '../../api/reviewer';

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

export const loadReviewInfo = createAction<{ reviewId: ReviewId }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<ReviewInfo>('LOADED_REVIEW_INFO');

export interface RememberRevisionArgs {
    reviewId: ReviewId;
    head: string;
    base: string;
}

export const rememberRevision = createAction<RememberRevisionArgs>('REMEMBER_REVISION');

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
        reviewId: null,
        title: '',
        headCommit: '',
        baseCommit: ''
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
            rangeInfo: action.payload,
            selectedFile: null,
            selectedFileDiff: null
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