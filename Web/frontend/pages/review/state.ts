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
    ReviewAuthor,
    CommentReply,
    FileToReview,
    FilesToReview,
    ReviewFile,
} from '../../api/reviewer';
import { UserState } from "../../rootState";
import * as PathPairs from '../../pathPair';

export interface FileInfo {
    path: PathPairs.PathPair;
    diff: FileDiff;
    fileToReview: FileToReview;
}

export interface ReviewState {
    selectedFile: FileInfo;
    currentReview: ReviewInfo;
    reviewedFiles: PathPairs.List;
    unpublishedFileDiscussions: FileDiscussion[];
    unpublishedReviewDiscussions: ReviewDiscussion[];
    unpublishedResolvedDiscussions: string[]; // root comment id
    unpublishedReplies: CommentReply[];
    nextReplyId: number;
    nextDiscussionCommentId: number;
    filesReviewedByUser: PathPairs.List;
    reviewableFiles: ReviewFile[];
}

const createAction = actionCreatorFactory('REVIEW');

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
export const replyToComment = createAction<{ parentId: string, content: string }>('REPLY_TO_COMMENT');

const initial: ReviewState = {
    selectedFile: null,
    currentReview: {
        hasProvisionalRevision: false,
        pastRevisions: [],
        reviewId: null,
        title: '',
        headCommit: '',
        baseCommit: '',
        headRevision: '',
        state: 'opened',
        mergeStatus: 'unchecked',
        fileDiscussions: [],
        reviewDiscussions: [],
        files: {},
        fileMatrix: []
    },
    reviewedFiles: [],
    unpublishedFileDiscussions: [],
    unpublishedReviewDiscussions: [],
    nextDiscussionCommentId: 0,
    unpublishedResolvedDiscussions: [],
    unpublishedReplies: [],
    nextReplyId: 0,
    filesReviewedByUser: [],
    reviewableFiles: [],
};

export const reviewReducer = (state: ReviewState = initial, action: AnyAction): ReviewState => {
    if (selectFileForView.match(action)) {
        const { files } = state.currentReview;
        const file = Object.keys(files).map(x => files[x].review).find(x => PathPairs.equal(x.path, action.payload.path));
        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                path: action.payload.path,
                fileToReview: file,
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
        const files = Object.keys(action.payload.files).map(f => action.payload.files[f])

        const fileInCurrentRevision = files.filter(f => f.review.current == action.payload.headCommit || f.review.current == action.payload.headRevision);
        const reviewedFiles = fileInCurrentRevision.filter(f => !f.review.hasChanges).map(f => f.review.path)
        
        return {
            ...state,
            currentReview: action.payload,
            reviewedFiles: reviewedFiles,
            reviewableFiles: fileInCurrentRevision,
            unpublishedFileDiscussions: [],
            unpublishedReviewDiscussions: [],
            unpublishedResolvedDiscussions: [],
            unpublishedReplies: [],
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
            nextDiscussionCommentId: state.nextDiscussionCommentId + 1,
            unpublishedFileDiscussions: [
                ...state.unpublishedFileDiscussions,
                {
                    revision: state.currentReview.headRevision,
                    filePath: action.payload.path,
                    lineNumber: action.payload.lineNumber,
                    comment: {
                        state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
                        author: action.payload.currentUser,
                        content: action.payload.content,
                        children: [],
                        createdAt: '',
                        id: `FILE-${state.nextDiscussionCommentId}`
                    }
                }
            ]
        };
    }

    if (startReviewDiscussion.match(action)) {
        return {
            ...state,
            nextDiscussionCommentId: state.nextDiscussionCommentId + 1,
            unpublishedReviewDiscussions: [
                ...state.unpublishedReviewDiscussions,
                {
                    revision: state.currentReview.headRevision,
                    comment: {
                        state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
                        author: action.payload.currentUser,
                        content: action.payload.content,
                        children: [],
                        createdAt: '',
                        id: `REVIEW-${state.nextDiscussionCommentId}`
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

    if (replyToComment.match(action)) {
        return {
            ...state,
            nextReplyId: state.nextReplyId + 1,
            unpublishedReplies: [
                ...state.unpublishedReplies,
                {
                    id: 'REPLY-' + state.nextReplyId,
                    parentId: action.payload.parentId,
                    content: action.payload.content
                }
            ]
        };
    }

    return state;
}
