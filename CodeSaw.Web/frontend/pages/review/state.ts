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
    RevisionId,
    FileId,
    DiffDiscussions,
    CommentState,
} from '../../api/reviewer';
import { UserState } from "../../rootState";
import * as PathPairs from '../../pathPair';
import * as _ from 'lodash'

export enum DiscussionType {
    Comment,
    NeedsResolution,
    GoodWork
}

export interface FileInfo {
    path: PathPairs.PathPair;
    diff: FileDiff;
    fileId: FileId;
    fileToReview: FileToReview;
    range: {
        previous: RevisionId;
        current: RevisionId;
    };
    discussions: FileDiscussion[];
}

export interface FileReviewStatusChange {
    [revision: string]: FileId[];
}

export interface UnpublishedReview {
    unpublishedFileDiscussions: (FileDiscussion)[];
    unpublishedReviewDiscussions: (ReviewDiscussion)[];
    unpublishedResolvedDiscussions: string[];
    unpublishedReplies: CommentReply[];
    unpublishedReviewedFiles: FileReviewStatusChange;
    unpublishedUnreviewedFiles: FileReviewStatusChange;
    nextReplyId: number;
    nextDiscussionCommentId: number;
}

export interface ReviewState extends UnpublishedReview {
    selectedFile: FileInfo;
    currentReview: ReviewInfo;
    reviewedFiles: FileId[];
    vsCodeWorkspace: string;
}

const createAction = actionCreatorFactory('REVIEW');

export const selectFileForView = createAction<{ fileId: FileId }>('SELECT_FILE_FOR_VIEW');
export const changeFileRange = createAction<{ previous: RevisionId; current: RevisionId; }>('CHANGE_FILE_RANGE');
export const markEmptyFilesAsReviewed = createAction<{}>('MARK_EMPTY_FILES_AS_REVIEWED');

export const loadedFileDiff = createAction<{ diff: FileDiff; remappedDiscussions: DiffDiscussions }>('LOADED_FILE_DIFF');

export const loadReviewInfo = createAction<{ reviewId: ReviewId, fileToPreload?: string }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<{ info: ReviewInfo, unpublishedInfo: UnpublishedReview, vsCodeWorkspace: string }>('LOADED_REVIEW_INFO');

export const clearUnpublishedReviewInfo = createAction<{ reviewId: ReviewId }>("CLEAR_UNPUBLISHED_REVIEW");

export interface RememberRevisionArgs {
    reviewId: ReviewId;
    head: string;
    base: string;
}

export interface PublishReviewArgs {
    fileToLoad: FileId;
}

export const publishReview = createAction<PublishReviewArgs>('PUBLISH_REVIEW');

export const reviewFile = createAction<{ path: PathPairs.PathPair }>('REVIEW_FILE');
export const unreviewFile = createAction<{ path: PathPairs.PathPair }>('UNREVIEW_FILE');

export interface MergePullRequestArgs {
    reviewId: ReviewId;
    shouldRemoveBranch: boolean;
    commitMessage?: string;
}

export const mergePullRequest = createAction<MergePullRequestArgs>('MERGE_PULL_REQUEST');

export const startFileDiscussion = createAction<{ fileId: FileId; lineNumber: number; content: string; type: DiscussionType; currentUser: UserState }>('START_FILE_DISCUSSION');
export const startReviewDiscussion = createAction<{ content: string; type: DiscussionType; currentUser: UserState }>('START_REVIEW_DISCUSSION');

export const unresolveDiscussion = createAction<{ discussionId: string }>('UNRESOLVE_DISCUSSION');
export const resolveDiscussion = createAction<{ discussionId: string }>('RESOLVE_DISCUSSION');
export const replyToComment = createAction<{ parentId: string, content: string }>('REPLY_TO_COMMENT');
export const editUnpublishedComment = createAction<{ commentId: string, content: string }>('EDIT_UNPUBLISHED_COMMENT');
export const removeUnpublishedComment = createAction<{ commentId: string }>('REMOVE_UNPUBLISHED_COMMENT');

export const saveVSCodeWorkspace = createAction<{ vsCodeWorkspace: string }>('SAVE_VS_CODE_WORKSPACE');
export const loadedVsCodeWorkspace = createAction<{ vsCodeWorkspace: string }>('LOADED_VS_CODE_WORKSPACE');


const UnpublishedCommentPrefixes = {
    Review: "REVIEW-",
    File: "FILE-",
    Reply: "REPLY-"
};

export const IsCommentUnpublished = (commentId: string): boolean => {
    return Object.keys(UnpublishedCommentPrefixes).findIndex(key => commentId.startsWith(UnpublishedCommentPrefixes[key])) >= 0;
}


export const emptyUnpublishedReview: UnpublishedReview = {
    unpublishedFileDiscussions: [],
    unpublishedReviewDiscussions: [],
    unpublishedResolvedDiscussions: [],
    unpublishedReplies: [],
    unpublishedReviewedFiles: {},
    unpublishedUnreviewedFiles: {},
    nextDiscussionCommentId: 0,
    nextReplyId: 0,
}

const initial: ReviewState = {
    selectedFile: null,
    currentReview: {
        hasProvisionalRevision: false,
        pastRevisions: [],
        reviewId: null,
        title: '',
        projectPath: '',
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
        isAuthor: false,
    },
    reviewedFiles: [],
    ...emptyUnpublishedReview,
    vsCodeWorkspace: ''
};

export const resolveRevision = (state: ReviewInfo, revision: RevisionId) => {
    if (revision == 'base') {
        return { base: state.baseCommit, head: state.baseCommit };
    };

    if (revision == state.headCommit) {
        return { base: state.baseCommit, head: state.headCommit };
    }

    if (revision == 'provisional') {
        return { base: state.baseCommit, head: state.headCommit };
    }

    const r = parseInt(revision.toString());

    const pastRevision = state.pastRevisions.find(x => x.number == r);

    return {
        base: pastRevision.base,
        head: pastRevision.head
    };
}


export const upgradeUnpublishedReview = (current: ReviewInfo, review: UnpublishedReview): UnpublishedReview => {
    const knownRevisions = current.pastRevisions.map(r => r.number as RevisionId);
    if (current.hasProvisionalRevision) {
        knownRevisions.push(current.headRevision as RevisionId);
    }

    const fileDiscussions = review.unpublishedFileDiscussions.map(fd => {
        const idx = knownRevisions.indexOf(fd.revision);
        if (idx != -1) {
            return fd;
        }

        return {
            ...fd,
            revision: current.headRevision
        };
    });

    const reviewDiscussions = review.unpublishedReviewDiscussions.map(fd => {
        const idx = knownRevisions.indexOf(fd.revision);
        if (idx != -1) {
            return fd;
        }

        return {
            ...fd,
            revision: current.headRevision
        };
    });

    const reviewedFiles: FileReviewStatusChange = {};

    for (const revision of Object.keys(review.unpublishedReviewedFiles)) {
        const idx = knownRevisions.indexOf(revision);
        if (idx != -1) {
            reviewedFiles[revision] = review.unpublishedReviewedFiles[revision];
        } else {
            reviewedFiles[current.headRevision] = review.unpublishedReviewedFiles[revision];
        }
    }

    const unreviewedFiles: FileReviewStatusChange = {};

    for (const revision of Object.keys(review.unpublishedUnreviewedFiles)) {
        const idx = knownRevisions.indexOf(revision);
        if (idx != -1) {
            unreviewedFiles[revision] = review.unpublishedUnreviewedFiles[revision];
        } else {
            unreviewedFiles[current.headRevision] = review.unpublishedUnreviewedFiles[revision];
        }
    }

    return {
        nextDiscussionCommentId: review.nextDiscussionCommentId,
        nextReplyId: review.nextReplyId,
        unpublishedFileDiscussions: fileDiscussions,
        unpublishedReplies: review.unpublishedReplies,
        unpublishedResolvedDiscussions: review.unpublishedResolvedDiscussions,
        unpublishedReviewDiscussions: reviewDiscussions,
        unpublishedReviewedFiles: reviewedFiles,
        unpublishedUnreviewedFiles: unreviewedFiles

    };
}

export const reviewReducer = (state: ReviewState = initial, action: AnyAction): ReviewState => {
    if (selectFileForView.match(action)) {
        const file = state.currentReview.filesToReview.find(f => f.fileId == action.payload.fileId);

        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                fileId: file.fileId,
                path: file.reviewFile,
                fileToReview: file,
                range: {
                    previous: file.previous,
                    current: file.current
                },
                discussions: []
            }
        };
    }

    if (changeFileRange.match(action)) {
        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                range: {
                    previous: action.payload.previous,
                    current: action.payload.current
                }
            }
        };
    }

    if (loadedFileDiff.match(action)) {
        const remappedDiscussions: FileDiscussion[] = [];

        for (let discussionRef of action.payload.remappedDiscussions.remapped) {
            const discussion = state.currentReview.fileDiscussions.find(d => d.id == discussionRef.discussionId);
            remappedDiscussions.push({
                ...discussion,
                lineNumber: discussionRef.lineNumber,
                revision: discussionRef.side == 'left' ? state.selectedFile.fileToReview.previous : state.selectedFile.fileToReview.current
            });
        }

        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                diff: action.payload.diff,
                discussions: remappedDiscussions
            },
        };
    }

    if (loadedReviewInfo.match(action)) {
        const unpublished = upgradeUnpublishedReview(action.payload.info, action.payload.unpublishedInfo);
        const reviewedFiles = action.payload.info.filesToReview.filter(f => f.current == f.previous);

        const getChangedFilesPaths = (changeStatus: FileReviewStatusChange) => Object.keys(changeStatus)
            .map(key => changeStatus[key])
            .reduce((a, b) => a.concat(b), []);

        const unpublishedReviewedFiles = getChangedFilesPaths(unpublished.unpublishedReviewedFiles);
        const unpublishedUnreviewedFiles = getChangedFilesPaths(unpublished.unpublishedUnreviewedFiles);

        const reviewedFileFinal =
            _.difference(reviewedFiles.map(f => f.fileId), unpublishedUnreviewedFiles)
                .concat(unpublishedReviewedFiles);

        return {
            ...state,
            currentReview: action.payload.info,
            reviewedFiles: reviewedFileFinal,
            ...unpublished,
            selectedFile: null,
            vsCodeWorkspace: action.payload.vsCodeWorkspace
        };
    }

    if (reviewFile.match(action)) {
        if (state.currentReview.isAuthor) {
            return state;
        }

        const file = state.currentReview.filesToReview.find(f => f.reviewFile == action.payload.path);

        if (state.reviewedFiles.indexOf(file.fileId) >= 0) {
            return state;
        }

        let reviewList = (state.unpublishedReviewedFiles[file.current] || []).concat([]);
        let unreviewList = (state.unpublishedUnreviewedFiles[file.current] || []).concat([]);

        const fileId = file.fileId;

        const idxInReviewed = reviewList.findIndex(f => f == fileId);
        const idxInUnreviewed = unreviewList.findIndex(f => f == fileId);

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
                file.fileId
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

        if (state.reviewedFiles.indexOf(file.fileId) == -1) {
            return state;
        }

        let reviewList = (state.unpublishedReviewedFiles[file.current] || []).concat([]);
        let unreviewList = (state.unpublishedUnreviewedFiles[file.current] || []).concat([]);

        const fileId = file.fileId;

        const idxInReviewed = reviewList.findIndex(f => f == fileId);
        const idxInUnreviewed = unreviewList.findIndex(f => f == fileId);

        if (idxInUnreviewed == -1 && idxInReviewed >= 0) {
            reviewList.splice(idxInReviewed, 1);
        } else if (idxInUnreviewed == -1 && idxInReviewed == -1) {
            unreviewList = [...unreviewList, fileId];
        } else {
            throw new Error('Holy crap...');
        }

        return {
            ...state,
            reviewedFiles: state.reviewedFiles.filter(v => v != fileId),
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

    const DISCUSSION_TYPE_TO_STATUS = {
        [DiscussionType.Comment]: 'NoActionNeeded' as CommentState,
        [DiscussionType.NeedsResolution]: 'NeedsResolution' as CommentState,
        [DiscussionType.GoodWork]: 'GoodWork' as CommentState
    }

    if (startFileDiscussion.match(action)) {
        return {
            ...state,
            nextDiscussionCommentId: state.nextDiscussionCommentId + 1,
            unpublishedFileDiscussions: [
                ...state.unpublishedFileDiscussions,
                {
                    id: `${UnpublishedCommentPrefixes.File}${state.nextDiscussionCommentId}`,
                    revision: state.selectedFile.fileToReview.current,
                    fileId: action.payload.fileId,
                    lineNumber: action.payload.lineNumber,
                    state: DISCUSSION_TYPE_TO_STATUS[action.payload.type],
                    canResolve: true,
                    comment: {
                        author: action.payload.currentUser,
                        content: action.payload.content,
                        children: [],
                        createdAt: '',
                        id: `${UnpublishedCommentPrefixes.File}${state.nextDiscussionCommentId}`
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
                    id: `${UnpublishedCommentPrefixes.Review}${state.nextDiscussionCommentId}`,
                    revision: state.currentReview.headRevision,
                    state: DISCUSSION_TYPE_TO_STATUS[action.payload.type],
                    canResolve: true,
                    comment: {
                        author: action.payload.currentUser,
                        content: action.payload.content,
                        children: [],
                        createdAt: '',
                        id: `${UnpublishedCommentPrefixes.Review}${state.nextDiscussionCommentId}`
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
                    id: UnpublishedCommentPrefixes.Reply + state.nextReplyId,
                    parentId: action.payload.parentId,
                    content: action.payload.content
                }
            ]
        };
    }

    if (editUnpublishedComment.match(action)) {
        if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Reply)) {
            const indexToRemove = state.unpublishedReplies.findIndex(reply => reply.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            const newReply: CommentReply = {
                ...state.unpublishedReplies[indexToRemove],
                content: action.payload.content
            };

            return {
                ...state,
                unpublishedReplies: [...state.unpublishedReplies.slice(0, indexToRemove), newReply, ...state.unpublishedReplies.slice(indexToRemove + 1)]
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.File)) {
            const indexToRemove = state.unpublishedFileDiscussions.findIndex(discussion => discussion.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            const newDiscussion: FileDiscussion = {
                ...state.unpublishedFileDiscussions[indexToRemove],
                comment: {
                    ...state.unpublishedFileDiscussions[indexToRemove].comment,
                    content: action.payload.content
                }
            };

            return {
                ...state,
                unpublishedFileDiscussions: [...state.unpublishedFileDiscussions.slice(0, indexToRemove), newDiscussion, ...state.unpublishedFileDiscussions.slice(indexToRemove + 1)]
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Review)) {
            const indexToRemove = state.unpublishedReviewDiscussions.findIndex(discussion => discussion.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            const newDiscussion: ReviewDiscussion = {
                ...state.unpublishedReviewDiscussions[indexToRemove],
                comment: {
                    ...state.unpublishedReviewDiscussions[indexToRemove].comment,
                    content: action.payload.content
                }
            };

            return {
                ...state,
                unpublishedReviewDiscussions: [...state.unpublishedReviewDiscussions.slice(0, indexToRemove), newDiscussion, ...state.unpublishedReviewDiscussions.slice(indexToRemove + 1)]
            };
        }
    }

    if (removeUnpublishedComment.match(action)) {
        if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Reply)) {
            const indexToRemove = state.unpublishedReplies.findIndex(reply => reply.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            return {
                ...state,
                unpublishedReplies: [...state.unpublishedReplies.slice(0, indexToRemove), ...state.unpublishedReplies.slice(indexToRemove + 1)]
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.File)) {
            const indexToRemove = state.unpublishedFileDiscussions.findIndex(discussion => discussion.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            return {
                ...state,
                unpublishedFileDiscussions: [...state.unpublishedFileDiscussions.slice(0, indexToRemove), ...state.unpublishedFileDiscussions.slice(indexToRemove + 1)]
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Review)) {
            const indexToRemove = state.unpublishedReviewDiscussions.findIndex(discussion => discussion.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            return {
                ...state,
                unpublishedReviewDiscussions: [...state.unpublishedReviewDiscussions.slice(0, indexToRemove), ...state.unpublishedReviewDiscussions.slice(indexToRemove + 1)]
            };
        }
    }

    if (loadedVsCodeWorkspace.match(action)) {
        return {
            ...state,
            vsCodeWorkspace: action.payload.vsCodeWorkspace
        }
    }


    return state;
}
