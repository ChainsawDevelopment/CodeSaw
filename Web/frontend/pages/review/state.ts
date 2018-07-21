import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';
import {
    RevisionRangeInfo,
    FileDiff,
    ReviewInfo,
    RevisionRange,
    ReviewId,
    ChangedFile,
    Comment,
    FileDiscussion,
    ReviewDiscussion,
    ReviewAuthor
} from '../../api/reviewer';
import { UserState } from "../../rootState";
import * as PathPairs from '../../pathPair';

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
    unpublishedFileDiscussions: FileDiscussion[];
    unpublishedReviewDiscussions: ReviewDiscussion[];
    unpublishedResolvedDiscussions: string[]; // root comment id
}

const createAction = actionCreatorFactory('REVIEW');

export interface SelectCurrentRevisions {
    range: RevisionRange;
    fileToLoad?: string;
}
export const selectCurrentRevisions = createAction<SelectCurrentRevisions>('SELECT_CURRENT_REVISIONS');
export const loadedRevisionsRangeInfo = createAction<RevisionRangeInfo>('LOADED_REVISION_RANGE_INFO');

export const selectFileForView = createAction<{ path: PathPairs.PathPair }>('SELECT_FILE_FOR_VIEW');

export const loadedFileDiff = createAction<FileDiff>('LOADED_FILE_DIFF');

export const loadReviewInfo = createAction<{ reviewId: ReviewId, fileToPreload?: string }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<ReviewInfo>('LOADED_REVIEW_INFO');

export interface RememberRevisionArgs {
    reviewId: ReviewId;
    head: string;
    base: string;
}

export interface CreateGitLabLinkArgs {
    reviewId: ReviewId;
}

export interface PublishReviewArgs {
    fileToLoad: string;
}

export const createGitLabLink = createAction<CreateGitLabLinkArgs>('CREATE_GITLAB_LINK');

export const publishReview = createAction<PublishReviewArgs>('PUBLISH_REVIEW');

export const reviewFile = createAction<{ path: PathPairs.PathPair }>('REVIEW_FILE');
export const unreviewFile = createAction<{ path: PathPairs.PathPair }>('UNREVIEW_FILE');

export interface MergePullRequestArgs {
    reviewId: ReviewId;
    shouldRemoveBranch: boolean;
    commitMessage?: string;
}

export const mergePullRequest = createAction<MergePullRequestArgs>('MERGE_PULL_REQUEST');

export const startFileDiscussion = createAction<{ path: PathPairs.PathPair; lineNumber: number; content: string; needsResolution: boolean, currentUser: UserState }>('START_FILE_DISCUSSION');
export const startReviewDiscussion = createAction<{ content: string; needsResolution: boolean, currentUser: UserState }>('START_REVIEW_DISCUSSION');

export const unresolveDiscussion = createAction<{ rootCommentId: string }>('UNRESOLVE_DISCUSSION');
export const resolveDiscussion = createAction<{ rootCommentId: string }>('RESOLVE_DISCUSSION');

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
        baseCommit: '',
        webUrl: '',
        state: 'opened',
        mergeStatus: 'unchecked',
        reviewSummary: [],
        fileDiscussions: [],
        reviewDiscussions: []
    },
    reviewedFiles: [],
    unpublishedFileDiscussions: [],
    unpublishedReviewDiscussions: [],
    unpublishedResolvedDiscussions: []
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
                ...state.selectedFile,
                path: action.payload.path,
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
            currentReview: action.payload,
            unpublishedFileDiscussions: [],
            unpublishedReviewDiscussions: [],
            unpublishedResolvedDiscussions: []
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

    const findCommentById = (id: string, comments: Comment[]): Comment => {
        for (const comment of comments) {
            if (comment.id === id) {
                return comment;
            }

            const childrenResult = findCommentById(id, comment.children);
            if (childrenResult !== null) {
                return childrenResult;
            }
        }

        return null;
    }

    if (startFileDiscussion.match(action)) {
        return {
            ...state,
            unpublishedFileDiscussions: [
                ...state.unpublishedFileDiscussions,
                {
                    revision: state.range.current,
                    filePath: action.payload.path,
                    lineNumber: action.payload.lineNumber,
                    comment: {
                        state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
                        author: action.payload.currentUser,
                        content: action.payload.content,
                        children: [],
                        createdAt: '',
                        id: (Math.max(0, ...state.unpublishedFileDiscussions.map(x => Number.parseInt(x.comment.id))) + 1).toString()
                    }
                }
            ]
        };
    }

    if (startReviewDiscussion.match(action)) {
        return {
            ...state,
            unpublishedReviewDiscussions: [
                ...state.unpublishedReviewDiscussions,
                {
                    revision: state.range.current,
                    comment: {
                        state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
                        author: action.payload.currentUser,
                        content: action.payload.content,
                        children: [],
                        createdAt: '',
                        id: (Math.max(0, ...state.unpublishedReviewDiscussions.map(x => Number.parseInt(x.comment.id))) + 1).toString()
                    }
                }
            ]
        };
    }

    if (unresolveDiscussion.match(action)) {
        return {
            ...state,
            unpublishedResolvedDiscussions: state.unpublishedResolvedDiscussions.filter(id => id != action.payload.rootCommentId)
        };
    }

    if (resolveDiscussion.match(action)) {
        if (state.unpublishedResolvedDiscussions.indexOf(action.payload.rootCommentId) >= 0) {
            return state;
        }

        return {
            ...state,
            unpublishedResolvedDiscussions: [
                ...state.unpublishedResolvedDiscussions,
                action.payload.rootCommentId
            ]
        };
    }

    return state;
}
