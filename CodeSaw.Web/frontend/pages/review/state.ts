import { actionCreatorFactory, AnyAction } from 'typescript-fsa';

import {
    FileDiff,
    ReviewInfo,
    ReviewId,
    Comment,
    FileDiscussion,
    ReviewDiscussion,
    CommentReply,
    FileToReview,
    RevisionId
} from '../../api/reviewer';
import { UserState } from "../../rootState";
import * as PathPairs from '../../pathPair';

export interface FileInfo {
    path: PathPairs.PathPair;
    diff: FileDiff;
    fileToReview: FileToReview;
    range: {
        previous: {
            base: string;
            head: string;
        };
        current: {
            base: string;
            head: string
        };
    }
}

export interface FileReviewStatusChange {
    [revision: string]: PathPairs.List;
}

export interface UnpublishedReview {
    unpublishedFileDiscussions: (FileDiscussion)[];
    unpublishedReviewDiscussions: (ReviewDiscussion)[];
    unpublishedResolvedDiscussions: string[]; 
    unpublishedReplies: CommentReply[];
    unpublishedReviewedFiles: FileReviewStatusChange;
    unpublishedUnreviewedFiles: FileReviewStatusChange;    
}

export interface ReviewState extends UnpublishedReview {
    selectedFile: FileInfo;
    currentReview: ReviewInfo;
    reviewedFiles: PathPairs.List;
    nextReplyId: number;
    nextDiscussionCommentId: number;
}

const createAction = actionCreatorFactory('REVIEW');

export const selectFileForView = createAction<{ path: PathPairs.PathPair }>('SELECT_FILE_FOR_VIEW');

export const loadedFileDiff = createAction<FileDiff>('LOADED_FILE_DIFF');

export const loadReviewInfo = createAction<{ reviewId: ReviewId, fileToPreload?: string }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<{ info: ReviewInfo, unpublishedInfo: UnpublishedReview}>('LOADED_REVIEW_INFO');

export const clearUnpublishedReviewInfo = createAction<{reviewId: ReviewId}>("CLEAR_UNPUBLISHED_REVIEW");

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

export const unresolveDiscussion = createAction<{ discussionId: string }>('UNRESOLVE_DISCUSSION');
export const resolveDiscussion = createAction<{ discussionId: string }>('RESOLVE_DISCUSSION');
export const replyToComment = createAction<{ parentId: string, content: string }>('REPLY_TO_COMMENT');

export const emptyUnpublishedReview : UnpublishedReview = {
    unpublishedFileDiscussions: [],
    unpublishedReviewDiscussions: [],
    unpublishedResolvedDiscussions: [],
    unpublishedReplies: [],
    unpublishedReviewedFiles: {},
    unpublishedUnreviewedFiles: {},
}

const initial: ReviewState = {
    selectedFile: null,
    currentReview: {
        hasProvisionalRevision: false,
        pastRevisions: [],
        reviewId: null,
        title: '',
        description: '',
        headCommit: '',
        baseCommit: '',
        webUrl: '',
        headRevision: '',
        state: 'opened',
        mergeStatus: 'unchecked',
        fileDiscussions: [],
        reviewDiscussions: [],
        fileMatrix: [],
        filesToReview: [],
        buildStatuses: [],
        sourceBranch: '',
        targetBranch: '',
        author: { username: "", name: "", avatarUrl: "" },
        reviewFinished: false,
        isAuthor: false
    },
    reviewedFiles: [],
    nextDiscussionCommentId: 0,
    nextReplyId: 0,
    ...emptyUnpublishedReview,
};

const resolveRevision = (state: ReviewInfo, revision: RevisionId) => {
    if (revision == 'base') {
        return { base: state.baseCommit, head: state.baseCommit };
    };

    if (revision == state.headCommit) {
        return { base: state.baseCommit, head: state.headCommit };
    }

    const r = parseInt(revision.toString());

    const pastRevision = state.pastRevisions.find(x => x.number == r);

    return {
        base: pastRevision.base,
        head: pastRevision.head
    };
}

export const reviewReducer = (state: ReviewState = initial, action: AnyAction): ReviewState => {
    if (selectFileForView.match(action)) {
        const file = state.currentReview.filesToReview.find(f => PathPairs.equal(f.reviewFile, action.payload.path));

        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                path: action.payload.path,
                fileToReview: file,
                range: {
                    previous: resolveRevision(state.currentReview, file.previous),
                    current: resolveRevision(state.currentReview, file.current)
                }
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
        const reviewedFiles = action.payload.info.filesToReview.filter(f => f.current == f.previous);

        const getChangedFilesPaths = (changeStatus: FileReviewStatusChange) => Object.keys(changeStatus)
            .map(key => changeStatus[key])
            .reduce((a,b) => a.concat(b), []);

        const unpublishedReviewedFiles = getChangedFilesPaths(action.payload.unpublishedInfo.unpublishedReviewedFiles);
        const unpublishedUnreviewedFiles = getChangedFilesPaths(action.payload.unpublishedInfo.unpublishedUnreviewedFiles);

        const reviewedFileFinal = 
            PathPairs.subtract(
                reviewedFiles.map(f => f.reviewFile),
                unpublishedUnreviewedFiles
            )
            .concat(unpublishedReviewedFiles);

        return {
            ...state,
            currentReview: action.payload.info,
            reviewedFiles: reviewedFileFinal,
            ...action.payload.unpublishedInfo,
            selectedFile: null
        };
    }

    if (reviewFile.match(action)) {
        const file = state.currentReview.filesToReview.find(f => f.reviewFile == action.payload.path);

        if (PathPairs.contains(state.reviewedFiles, file.reviewFile)) {
            return state;
        }

        let reviewList = (state.unpublishedReviewedFiles[file.current] || []).concat([]);
        let unreviewList = (state.unpublishedUnreviewedFiles[file.current] || []).concat([]);

        const fileId = PathPairs.make(file.diffFile.newPath);

        const idxInReviewed = reviewList.findIndex(f => PathPairs.equal(f, fileId));
        const idxInUnreviewed = unreviewList.findIndex(f => PathPairs.equal(f, fileId));

        if (idxInUnreviewed >= 0 && idxInReviewed == -1) {
            unreviewList.splice(idxInUnreviewed, 1);
        } else if (idxInUnreviewed == -1 && idxInReviewed == -1) {
            reviewList = [...reviewList, fileId];
        } else {
            throw new Error('Holy crap...');
        }

        return {
            ...state,
            reviewedFiles: [
                ...state.reviewedFiles,
                file.reviewFile
            ],
            unpublishedReviewedFiles: {
                ...state.unpublishedReviewedFiles,
                [file.current]: reviewList
            },
            unpublishedUnreviewedFiles: {
                ...state.unpublishedUnreviewedFiles,
                [file.current]: unreviewList
            },
        };
    }

    if (unreviewFile.match(action)) {
        const file = state.currentReview.filesToReview.find(f => f.reviewFile == action.payload.path);

        if (!PathPairs.contains(state.reviewedFiles, file.reviewFile)) {
            return state;
        }

        let reviewList = (state.unpublishedReviewedFiles[file.current] || []).concat([]);
        let unreviewList = (state.unpublishedUnreviewedFiles[file.current] || []).concat([]);

        const fileId = PathPairs.make(file.diffFile.newPath);

        const idxInReviewed = reviewList.findIndex(f => PathPairs.equal(f, fileId));
        const idxInUnreviewed = unreviewList.findIndex(f => PathPairs.equal(f, fileId));

        if (idxInUnreviewed == -1 && idxInReviewed >= 0) {
            reviewList.splice(idxInReviewed, 1);
        } else if (idxInUnreviewed == -1 && idxInReviewed == -1) {
            unreviewList = [...unreviewList, fileId];
        } else {
            throw new Error('Holy crap...');
        }

        return {
            ...state,
            reviewedFiles: state.reviewedFiles.filter(v => !PathPairs.equal(v, file.reviewFile)),
            unpublishedReviewedFiles: {
                ...state.unpublishedReviewedFiles,
                [file.current]: reviewList
            },
            unpublishedUnreviewedFiles: {
                ...state.unpublishedUnreviewedFiles,
                [file.current]: unreviewList
            },
        };
    }

    if (clearUnpublishedReviewInfo.match(action)) {
        return {
            ...state,
            ...emptyUnpublishedReview
        }
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
                    id: `FILE-${state.nextDiscussionCommentId}`,
                    revision: state.selectedFile.fileToReview.current,
                    filePath: action.payload.path,
                    lineNumber: action.payload.lineNumber,
                    state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
                    canResolve: true,
                    comment: {
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
                    id: `REVIEW-${state.nextDiscussionCommentId}`,
                    revision: state.currentReview.headRevision,
                    state: action.payload.needsResolution ? 'NeedsResolution' : 'NoActionNeeded',
                    canResolve: true,
                    comment: {
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
            unpublishedResolvedDiscussions: state.unpublishedResolvedDiscussions.filter(id => id != action.payload.discussionId)
        };
    }

    if (resolveDiscussion.match(action)) {
        if (state.unpublishedResolvedDiscussions.indexOf(action.payload.discussionId) >= 0) {
            return state;
        }

        return {
            ...state,
            unpublishedResolvedDiscussions: [
                ...state.unpublishedResolvedDiscussions,
                action.payload.discussionId
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
