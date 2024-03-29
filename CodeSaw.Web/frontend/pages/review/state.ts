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
    FileId,
    DiffDiscussions,
    CommentState,
} from '../../api/reviewer';
import { UserState } from '../../rootState';
import * as PathPairs from '../../pathPair';
import * as _ from 'lodash';
import { RevisionId, LocalRevisionId } from '@api/revisionId';
import { upgradeReview } from './upgradeReview';

export enum DiscussionType {
    Comment,
    NeedsResolution,
    GoodWork,
}

export const DiscussionState = {
    GoodWork: 'GoodWork' as CommentState,
    NoActionNeeded: 'NoActionNeeded' as CommentState,
    NeedsResolution: 'NeedsResolution' as CommentState,
    Resolved: 'Resolved' as CommentState,
    ResolvePending: 'ResolvePending' as CommentState,
};

export interface FileInfo {
    path: PathPairs.PathPair;
    diff: FileDiff;
    fileId: FileId;
    fileToReview: FileToReview;
    range: {
        previous: LocalRevisionId;
        current: LocalRevisionId;
    };
    discussions: FileDiscussion[];
}

export interface FileReviewStatusChange {
    revision: LocalRevisionId;
    fileId: FileId;
}

export interface UnpublishedReview {
    headCommit: string;
    baseCommit: string;
    unpublishedFileDiscussions: FileDiscussion[];
    unpublishedReviewDiscussions: ReviewDiscussion[];
    unpublishedResolvedDiscussions: string[];
    unpublishedReplies: CommentReply[];
    unpublishedReviewedFiles: FileReviewStatusChange[];
    unpublishedUnreviewedFiles: FileReviewStatusChange[];
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
export const changeFileRange = createAction<{
    previous: LocalRevisionId;
    current: LocalRevisionId;
}>('CHANGE_FILE_RANGE');
export const markEmptyFilesAsReviewed = createAction<{}>('MARK_EMPTY_FILES_AS_REVIEWED');

export const loadedFileDiff = createAction<{ diff: FileDiff; remappedDiscussions: DiffDiscussions }>(
    'LOADED_FILE_DIFF',
);

export const loadReviewInfo = createAction<{ reviewId: ReviewId; fileToPreload?: string }>('LOAD_REVIEW_INFO');
export const loadedReviewInfo = createAction<{
    info: ReviewInfo;
    unpublishedInfo: UnpublishedReview;
    fileIdMap: { [fileId: string]: string };
    vsCodeWorkspace: string;
}>('LOADED_REVIEW_INFO');

export const clearUnpublishedReviewInfo = createAction<{ reviewId: ReviewId }>('CLEAR_UNPUBLISHED_REVIEW');

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

export const startFileDiscussion = createAction<{
    fileId: FileId;
    lineNumber: number;
    content: string;
    type: DiscussionType;
    currentUser: UserState;
}>('START_FILE_DISCUSSION');
export const startReviewDiscussion = createAction<{ content: string; type: DiscussionType; currentUser: UserState }>(
    'START_REVIEW_DISCUSSION',
);

export const unresolveDiscussion = createAction<{ discussionId: string }>('UNRESOLVE_DISCUSSION');
export const resolveDiscussion = createAction<{ discussionId: string }>('RESOLVE_DISCUSSION');
export const replyToComment = createAction<{ parentId: string; content: string }>('REPLY_TO_COMMENT');
export const editUnpublishedComment = createAction<{ commentId: string; content: string }>('EDIT_UNPUBLISHED_COMMENT');
export const removeUnpublishedComment = createAction<{ commentId: string }>('REMOVE_UNPUBLISHED_COMMENT');

export const saveVSCodeWorkspace = createAction<{ vsCodeWorkspace: string }>('SAVE_VS_CODE_WORKSPACE');
export const loadedVsCodeWorkspace = createAction<{ vsCodeWorkspace: string }>('LOADED_VS_CODE_WORKSPACE');

const UnpublishedCommentPrefixes = {
    Review: 'REVIEW-',
    File: 'FILE-',
    Reply: 'REPLY-',
};

export const IsCommentUnpublished = (commentId: string): boolean => {
    return (
        Object.keys(UnpublishedCommentPrefixes).findIndex((key) =>
            commentId.startsWith(UnpublishedCommentPrefixes[key]),
        ) >= 0
    );
};

export const emptyUnpublishedReview: UnpublishedReview = {
    headCommit: '',
    baseCommit: '',
    unpublishedFileDiscussions: [],
    unpublishedReviewDiscussions: [],
    unpublishedResolvedDiscussions: [],
    unpublishedReplies: [],
    unpublishedReviewedFiles: [],
    unpublishedUnreviewedFiles: [],
    nextDiscussionCommentId: 0,
    nextReplyId: 0,
};

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
        headRevision: RevisionId.Provisional,
        state: 'opened',
        mergeStatus: 'unchecked',
        fileDiscussions: [],
        reviewDiscussions: [],
        fileMatrix: [],
        filesToReview: [],
        buildStatuses: [],
        sourceBranch: '',
        targetBranch: '',
        author: { username: '', name: '', avatarUrl: '' },
        reviewFinished: false,
        isAuthor: false,
        commits: [],
    },
    reviewedFiles: [],
    ...emptyUnpublishedReview,
    vsCodeWorkspace: '',
};

export const resolveRevision2 = (state: ReviewInfo, revision: LocalRevisionId): any => {
    if (RevisionId.isBase(revision)) {
        return { base: state.baseCommit, head: state.baseCommit };
    }

    if (RevisionId.isProvisional(revision)) {
        return { base: state.baseCommit, head: state.headCommit };
    }

    const pastRevision = state.pastRevisions.find((x) => x.number == revision.revision);

    return {
        base: pastRevision.base,
        head: pastRevision.head,
    };
};

export const reviewReducer = (state: ReviewState = initial, action: AnyAction): ReviewState => {
    if (selectFileForView.match(action)) {
        const file = state.currentReview.filesToReview.find((f) => f.fileId == action.payload.fileId);

        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                fileId: file.fileId,
                path: file.reviewFile,
                fileToReview: file,
                range: {
                    previous: file.previous,
                    current: file.current,
                },
                discussions: [],
            },
        };
    }

    if (changeFileRange.match(action)) {
        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                diff: null,
                range: {
                    previous: action.payload.previous,
                    current: action.payload.current,
                },
            },
        };
    }

    if (loadedFileDiff.match(action)) {
        const remappedDiscussions: FileDiscussion[] = [];

        for (const discussionRef of action.payload.remappedDiscussions.remapped) {
            const discussion = state.currentReview.fileDiscussions.find((d) => d.id == discussionRef.discussionId);
            remappedDiscussions.push({
                ...discussion,
                lineNumber: discussionRef.lineNumber,
                revision:
                    discussionRef.side == 'left' ? state.selectedFile.range.previous : state.selectedFile.range.current,
            });
        }

        return {
            ...state,
            selectedFile: {
                ...state.selectedFile,
                diff: action.payload.diff,
                discussions: remappedDiscussions,
            },
        };
    }

    if (loadedReviewInfo.match(action)) {
        const unpublished = upgradeReview(
            action.payload.info,
            action.payload.unpublishedInfo,
            action.payload.fileIdMap,
        );
        const reviewedFiles = action.payload.info.filesToReview.filter((f) => RevisionId.equal(f.previous, f.current));

        const getChangedFilesPaths = (changeStatus: FileReviewStatusChange[]) => changeStatus.map((c) => c.fileId);

        const unpublishedReviewedFiles2 = getChangedFilesPaths(unpublished.unpublishedReviewedFiles);
        const unpublishedUnreviewedFiles2 = getChangedFilesPaths(unpublished.unpublishedUnreviewedFiles);

        const reviewedFileFinal = _.difference(
            reviewedFiles.map((f) => f.fileId),
            unpublishedUnreviewedFiles2,
        ).concat(unpublishedReviewedFiles2);

        return {
            ...state,
            currentReview: action.payload.info,
            reviewedFiles: reviewedFileFinal,
            ...unpublished,
            headCommit: action.payload.info.headCommit,
            baseCommit: action.payload.info.baseCommit,
            selectedFile: null,
            vsCodeWorkspace: action.payload.vsCodeWorkspace,
        };
    }

    if (reviewFile.match(action)) {
        if (state.currentReview.isAuthor) {
            return state;
        }

        const file = state.currentReview.filesToReview.find((f) => f.reviewFile == action.payload.path);

        if (state.reviewedFiles.indexOf(file.fileId) >= 0) {
            return state;
        }

        let reviewList = state.unpublishedReviewedFiles;
        const unreviewList = state.unpublishedUnreviewedFiles;

        const fileId = file.fileId;

        const idxInReviewed2 = reviewList.findIndex(
            (f) => f.fileId == fileId && RevisionId.equal(f.revision, file.current),
        );
        const idxInUnreviewed2 = unreviewList.findIndex(
            (f) => f.fileId == fileId && RevisionId.equal(f.revision, file.current),
        );

        if (idxInUnreviewed2 >= 0 && idxInReviewed2 == -1) {
            unreviewList.splice(idxInUnreviewed2, 1);
        } else if (idxInUnreviewed2 == -1 && idxInReviewed2 == -1) {
            reviewList = [...reviewList, { fileId, revision: file.current }];
        } else {
            throw new Error('Holy crap...');
        }

        return {
            ...state,
            reviewedFiles: [...state.reviewedFiles, file.fileId],
            unpublishedReviewedFiles: reviewList,
            unpublishedUnreviewedFiles: unreviewList,
        };
    }

    if (unreviewFile.match(action)) {
        const file = state.currentReview.filesToReview.find((f) => f.reviewFile == action.payload.path);

        if (state.reviewedFiles.indexOf(file.fileId) == -1) {
            return state;
        }

        const reviewList = state.unpublishedReviewedFiles;
        let unreviewList = state.unpublishedUnreviewedFiles;

        const fileId = file.fileId;

        const idxInReviewed2 = reviewList.findIndex(
            (f) => f.fileId == fileId && RevisionId.equal(f.revision, file.current),
        );
        const idxInUnreviewed2 = unreviewList.findIndex(
            (f) => f.fileId == fileId && RevisionId.equal(f.revision, file.current),
        );

        if (idxInUnreviewed2 == -1 && idxInReviewed2 >= 0) {
            reviewList.splice(idxInReviewed2, 1);
        } else if (idxInUnreviewed2 == -1 && idxInReviewed2 == -1) {
            unreviewList = [...unreviewList, { fileId, revision: file.current }];
        } else {
            throw new Error('Holy crap...');
        }

        return {
            ...state,
            reviewedFiles: state.reviewedFiles.filter((v) => v != fileId),
            unpublishedReviewedFiles: reviewList,
            unpublishedUnreviewedFiles: unreviewList,
        };
    }

    if (clearUnpublishedReviewInfo.match(action)) {
        return {
            ...state,
            ...emptyUnpublishedReview,
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
    };

    const DISCUSSION_TYPE_TO_STATUS = {
        [DiscussionType.Comment]: DiscussionState.NoActionNeeded as CommentState,
        [DiscussionType.NeedsResolution]: DiscussionState.NeedsResolution as CommentState,
        [DiscussionType.GoodWork]: DiscussionState.GoodWork as CommentState,
    };

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
                        id: `${UnpublishedCommentPrefixes.File}${state.nextDiscussionCommentId}`,
                    },
                },
            ],
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
                        id: `${UnpublishedCommentPrefixes.Review}${state.nextDiscussionCommentId}`,
                    },
                },
            ],
        };
    }

    if (unresolveDiscussion.match(action)) {
        return {
            ...state,
            unpublishedResolvedDiscussions: state.unpublishedResolvedDiscussions.filter(
                (id) => id != action.payload.discussionId,
            ),
        };
    }

    if (resolveDiscussion.match(action)) {
        if (state.unpublishedResolvedDiscussions.indexOf(action.payload.discussionId) >= 0) {
            return state;
        }

        return {
            ...state,
            unpublishedResolvedDiscussions: [...state.unpublishedResolvedDiscussions, action.payload.discussionId],
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
                    content: action.payload.content,
                },
            ],
        };
    }

    if (editUnpublishedComment.match(action)) {
        if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Reply)) {
            const indexToRemove = state.unpublishedReplies.findIndex((reply) => reply.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            const newReply: CommentReply = {
                ...state.unpublishedReplies[indexToRemove],
                content: action.payload.content,
            };

            return {
                ...state,
                unpublishedReplies: [
                    ...state.unpublishedReplies.slice(0, indexToRemove),
                    newReply,
                    ...state.unpublishedReplies.slice(indexToRemove + 1),
                ],
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.File)) {
            const indexToRemove = state.unpublishedFileDiscussions.findIndex(
                (discussion) => discussion.id == action.payload.commentId,
            );
            if (indexToRemove === -1) {
                return state;
            }

            const newDiscussion: FileDiscussion = {
                ...state.unpublishedFileDiscussions[indexToRemove],
                comment: {
                    ...state.unpublishedFileDiscussions[indexToRemove].comment,
                    content: action.payload.content,
                },
            };

            return {
                ...state,
                unpublishedFileDiscussions: [
                    ...state.unpublishedFileDiscussions.slice(0, indexToRemove),
                    newDiscussion,
                    ...state.unpublishedFileDiscussions.slice(indexToRemove + 1),
                ],
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Review)) {
            const indexToRemove = state.unpublishedReviewDiscussions.findIndex(
                (discussion) => discussion.id == action.payload.commentId,
            );
            if (indexToRemove === -1) {
                return state;
            }

            const newDiscussion: ReviewDiscussion = {
                ...state.unpublishedReviewDiscussions[indexToRemove],
                comment: {
                    ...state.unpublishedReviewDiscussions[indexToRemove].comment,
                    content: action.payload.content,
                },
            };

            return {
                ...state,
                unpublishedReviewDiscussions: [
                    ...state.unpublishedReviewDiscussions.slice(0, indexToRemove),
                    newDiscussion,
                    ...state.unpublishedReviewDiscussions.slice(indexToRemove + 1),
                ],
            };
        }
    }

    if (removeUnpublishedComment.match(action)) {
        if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Reply)) {
            const indexToRemove = state.unpublishedReplies.findIndex((reply) => reply.id == action.payload.commentId);
            if (indexToRemove === -1) {
                return state;
            }

            return {
                ...state,
                unpublishedReplies: [
                    ...state.unpublishedReplies.slice(0, indexToRemove),
                    ...state.unpublishedReplies.slice(indexToRemove + 1),
                ],
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.File)) {
            const indexToRemove = state.unpublishedFileDiscussions.findIndex(
                (discussion) => discussion.id == action.payload.commentId,
            );
            if (indexToRemove === -1) {
                return state;
            }

            return {
                ...state,
                unpublishedFileDiscussions: [
                    ...state.unpublishedFileDiscussions.slice(0, indexToRemove),
                    ...state.unpublishedFileDiscussions.slice(indexToRemove + 1),
                ],
            };
        } else if (action.payload.commentId.startsWith(UnpublishedCommentPrefixes.Review)) {
            const indexToRemove = state.unpublishedReviewDiscussions.findIndex(
                (discussion) => discussion.id == action.payload.commentId,
            );
            if (indexToRemove === -1) {
                return state;
            }

            return {
                ...state,
                unpublishedReviewDiscussions: [
                    ...state.unpublishedReviewDiscussions.slice(0, indexToRemove),
                    ...state.unpublishedReviewDiscussions.slice(indexToRemove + 1),
                ],
            };
        }
    }

    if (loadedVsCodeWorkspace.match(action)) {
        return {
            ...state,
            vsCodeWorkspace: action.payload.vsCodeWorkspace,
        };
    }

    return state;
};
