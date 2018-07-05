import { actionCreatorFactory, AnyAction, isType } from 'typescript-fsa';
import { Guid } from 'guid-typescript';
import * as joda from 'js-joda';
import {
    RevisionRangeInfo,
    FileDiff,
    ReviewInfo,
    RevisionRange,
    ReviewId,
    ChangedFile,
    Comment
} from '../../api/reviewer';
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
    comments: Comment[];
}

const createAction = actionCreatorFactory('REVIEW');

export interface SelectCurrentRevisions {
    range: RevisionRange;
}
export const selectCurrentRevisions = createAction<SelectCurrentRevisions>('SELECT_CURRENT_REVISIONS');
export const loadedRevisionsRangeInfo = createAction<RevisionRangeInfo>('LOADED_REVISION_RANGE_INFO');

export const selectFileForView = createAction<{ path: PathPairs.PathPair }>('SELECT_FILE_FOR_VIEW');

export const loadedFileDiff = createAction<FileDiff>('LOADED_FILE_DIFF');

export const loadReviewInfo = createAction<{ reviewId: ReviewId, fileToPreload?: string }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<ReviewInfo>('LOADED_REVIEW_INFO');

export const loadComments = createAction<{}>('LOAD_COMMENTS');
export const loadedComments = createAction<Comment[]>('LOADED_COMMENTS');

export interface RememberRevisionArgs {
    reviewId: ReviewId;
    head: string;
    base: string;
}

export interface CreateGitLabLinkArgs {
    reviewId: ReviewId;
}

export const createGitLabLink = createAction<CreateGitLabLinkArgs>('CREATE_GITLAB_LINK');

export const publishReview = createAction<{}>('PUBLISH_REVIEW');

export const reviewFile = createAction<{ path: PathPairs.PathPair }>('REVIEW_FILE');
export const unreviewFile = createAction<{ path: PathPairs.PathPair }>('UNREVIEW_FILE');

export interface AddCommentArgs {
    parentId?: string;
    content: string;
    filePath: string;
    changeKey: string;
    needsResolution: boolean;
}

export const addComment = createAction<AddCommentArgs>('ADD_COMMENT');

export const resolveComment = createAction<{ commentId: string }>('RESOLVE_COMMENT');

export interface MergePullRequestArgs {
    reviewId: ReviewId;
    shouldRemoveBranch: boolean;
    commitMessage?: string;
}

export const mergePullRequest = createAction<MergePullRequestArgs>('MERGE_PULL_REQUEST');

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
        state: 'opened',
        mergeStatus: 'unchecked',
        reviewSummary: []
    },
    reviewedFiles: [],
    comments: []
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

    if (loadedComments.match(action)) {
        return {
            ...state,
            comments: action.payload
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

    if (addComment.match(action)) {
        const newComment: Comment = {
            author: 'NOT SUBMITTED',
            changeKey: action.payload.changeKey,
            content: action.payload.content,
            filePath: action.payload.filePath,
            state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
            createdAt: joda.LocalDateTime.now(joda.ZoneOffset.UTC).toString(),
            id: Guid.create().toString(),
            children: []
        }

        const commentsState = JSON.parse(JSON.stringify(state.comments)) as Comment[];
        if (action.payload.parentId) {
            const parentComment = findCommentById(action.payload.parentId, commentsState);
            parentComment.children.push(newComment);
        } else {
            commentsState.push(newComment);
        }

        return {
            ...state,
            comments: commentsState
        }
    }

    if (resolveComment.match(action)) {
        const commentsState = JSON.parse(JSON.stringify(state.comments)) as Comment[];
        const comment = findCommentById(action.payload.commentId, commentsState);
        comment.state = 'Resolved';

        return {
            ...state,
            comments: commentsState
        }
    }

    return state;
}
