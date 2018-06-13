import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';
import { RevisionRangeInfo, FileDiff, ReviewInfo, RevisionRange, ReviewId, ChangedFile } from '../../api/reviewer';
import  * as PathPairs from '../../pathPair';

export interface FileInfo {
    path: PathPairs.PathPair;
    diff: FileDiff;
    treeEntry: ChangedFile;
}

export interface ReviewState {
    range: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: FileInfo;
    currentReview: ReviewInfo;
    reviewedFiles: PathPairs.List;
}

const createAction = actionCreatorFactory('REVIEW');

export interface SelectCurrentRevisions {
    range: RevisionRange;
}
export const selectCurrentRevisions = createAction<SelectCurrentRevisions>('SELECT_CURRENT_REVISIONS');
export const loadedRevisionsRangeInfo = createAction<RevisionRangeInfo>('LOADED_REVISION_RANGE_INFO');

export const selectFileForView = createAction<{ path: PathPairs.PathPair }>('SELECT_FILE_FOR_VIEW');

export const loadedFileDiff = createAction<FileDiff>('LOADED_FILE_DIFF');

export const loadReviewInfo = createAction<{ reviewId: ReviewId }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<ReviewInfo>('LOADED_REVIEW_INFO');

export interface CreateGitLabLinkArgs {
    reviewId: ReviewId;
}

export const createGitLabLink = createAction<CreateGitLabLinkArgs>('CREATE_GITLAB_LINK');

export const publishReview = createAction<{}>('PUBLISH_REVIEW');

export const reviewFile = createAction<{ path: PathPairs.PathPair }>('REVIEW_FILE');
export const unreviewFile = createAction<{ path: PathPairs.PathPair }>('UNREVIEW_FILE');

const initial: ReviewState = {
    range: {
        previous: 'base',
        current: 'base'
    },
    rangeInfo: null,
    selectedFile: null,
    currentReview: {
        hasProvisionalRevision: false,
        pastRevisions: [],
        reviewId: null,
        title: '',
        headCommit: '',
        baseCommit: ''
    },
    reviewedFiles: []
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
            reviewedFiles: action.payload.filesReviewedByUser
        }
    }

    if (selectFileForView.match(action)) {
        const treeEntry = state.rangeInfo.changes.find(x => x.path.newPath == action.payload.path.newPath);
        return {
            ...state,
            selectedFile: {
                path: action.payload.path,
                diff: null,
                treeEntry: treeEntry
            }
        };
    }

    if (loadedFileDiff.match(action)) {
        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                diff: action.payload
            }
        };
    }

    if (loadedReviewInfo.match(action)) {
        return {
            ...state,
            currentReview: action.payload
        };
    }

    if (reviewFile.match(action)) {
        if (PathPairs.contains(state.reviewedFiles, action.payload.path)) {
            return state;
        }

        return {
            ...state,
            reviewedFiles: [
                ...state.reviewedFiles,
                action.payload.path
            ]
        };
    }

    if (unreviewFile.match(action)) {
        if (!PathPairs.contains(state.reviewedFiles, action.payload.path)) {
            return state;
        }

        return {
            ...state,
            reviewedFiles: state.reviewedFiles.filter(v => v.newPath != action.payload.path.newPath)
        };
    }

    return state;
} 