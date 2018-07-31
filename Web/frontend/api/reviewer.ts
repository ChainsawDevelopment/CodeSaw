import * as PathPairs from "../pathPair";
import { UserState } from "../rootState";

export type RevisionId = 'base' | number | string | 'provisional';

export interface RevisionRange {
    previous: RevisionId;
    current: RevisionId;
}

export interface ChangedFile {
    path: PathPairs.PathPair;
    renamedFile: boolean;
    deletedFile: boolean;
    newFile: boolean;
}

export interface RevisionRangeInfo {
    changes: ChangedFile[];
    commits: {
        current: {
            head: string;
            base: string
        };
        previous: {
            head: string;
            base: string
        }
    };
    filesReviewedByUser: PathPairs.List
}

export interface HunkLine {
    operation: 'Equal' | 'Insert' | 'Delete';
    classification: 'Unchanged' | 'BaseChange' | 'ReviewChange';
    line: number;
    text: string;
}

export interface HunkPosition {
    start: number;
    end: number;
    length: number;
}

export interface Hunk {
    lines: HunkLine[];
    newPosition: HunkPosition;
    oldPosition: HunkPosition;
}

export interface BinaryFileSizes {
    previousSize: number;
    currentSize: number;
}

export interface FileDiff {
    hunks: Hunk[];
    isBinaryFile: boolean;
    areBinaryEqual: boolean;
    binarySizes: BinaryFileSizes;
}

export interface ReviewId {
    projectId: number;
    reviewId: number;
}

export type ReviewAuthor = UserState;

export interface Review {
    reviewId: ReviewId;
    project: string;
    title: string;
    webUrl: string;
    changesCount: number;
    author: ReviewAuthor;
}

export type ReviewInfoState = 'opened' | "reopened" | "merged" | "closed";

export interface FileDiscussion
{
    revision: RevisionId;
    filePath: PathPairs.PathPair;
    lineNumber: number;
    comment: Comment;
}

export interface ReviewDiscussion
{
    revision: RevisionId;
    comment: Comment;
}

export interface ReviewInfo {
    reviewId: ReviewId;
    title: string;
    pastRevisions: {
        number: number;
        head: string;
        base: string;
    }[];
    hasProvisionalRevision: boolean;
    headCommit: string;
    baseCommit: string;
    webUrl: string;
    headRevision: RevisionId;
    state: ReviewInfoState;
    mergeStatus: 'can_be_merged' | 'cannot_be_merged' | 'unchecked';
    fileDiscussions: FileDiscussion[];
    reviewDiscussions: ReviewDiscussion[];
    files: ReviewFiles;
    fileMatrix: any;
}

export interface ReviewFiles {
    [file: string]: ReviewFile;
}

export interface ReviewFile {
    summary: {
        revisionReviewers: {
            [revision: number]: string[];
        }
    };
    review: FileToReview;
}

export interface CommentReply {
    id: string;
    parentId: string;
    content: string;
}

export interface ReviewSnapshot {
    reviewId: ReviewId;
    revision: {
        head: string,
        base: string
    };
    reviewedFiles: PathPairs.PathPair[];
    startedFileDiscussions: {
        temporaryId: string;
        file: PathPairs.PathPair;
        lineNumber: number;
        content: string;
        needsResolution: boolean;
    }[];
    startedReviewDiscussions: {
        temporaryId: string;
        needsResolution: boolean;
        content: string;
    }[];
    resolvedDiscussions: string[]; // root comment ids
    replies: CommentReply[];
}

export type CommentState = 'NoActionNeeded' | 'NeedsResolution' | 'Resolved' | 'ResolvePending';

export interface Comment {
    id: string;
    author: UserState;
    content: string;
    state: CommentState,
    createdAt: string;
    children: Comment[];
}

export interface ProjectInfo {
    id: number;
    namespace: string;
    name: string;
    canConfigureHooks: boolean;
}

export interface FileToReview {
    path: PathPairs.PathPair;
    previous: RevisionId;
    current: RevisionId;
    hasChanges: boolean;
    isRenamedFile: boolean;
    isNewFile: boolean;
    isDeletedFile: boolean;
}

export interface FilesToReview {
    filesToReview: FileToReview[];
}

const acceptJson = {
    headers: {
        'Accept': 'application/json'
    },
    credentials: 'include' as RequestCredentials
};

export class ReviewConcurrencyError extends Error {
    constructor() {
        super('Publish review concurrency issue');
    }
}

export class ReviewerApi {
    public getDiff = (reviewId: ReviewId, range: RevisionRange, path: PathPairs.PathPair): Promise<FileDiff> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/diff/${range.previous}/${range.current}?oldPath=${path.oldPath}&newPath=${path.newPath}`,
            acceptJson
        ).then(r => r.json());
    };

    public getReviews = (): Promise<Review[]> => {
        return fetch('/api/reviews', acceptJson)
            .then(r => r.json())
            .then(r => r as Review[]);
    };

    public getReviewInfo = (reviewId: ReviewId): Promise<ReviewInfo> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/info`, acceptJson)
            .then(r => r.json())
            .then(r => r as ReviewInfo);
            // .then(ri => {
            //     for (let item of ri.reviewSummary) {
            //         const converted = {};

            //         for (let rev of Object.keys(item.revisions)) {
            //             converted[parseInt(rev)] = item.revisions[rev];
            //         }

            //         item.revisions = converted;
            //     }

            //     return ri;
            // });
    }

    public createGitLabLink = (reviewId: ReviewId) => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/registerlink`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json'
            },
            method: 'POST'
        });
    }

    public publishReview = (review: ReviewSnapshot): Promise<void> => {
        const { reviewId, ...snapshot } = review;

        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/publish`,
            {
                ...acceptJson,
                headers: {
                    ...acceptJson.headers,
                    'Content-Type': 'application/json'
                },
                method: 'POST',
                body: JSON.stringify(snapshot)
            }
        ).then(r => {
            if (r.status == 409) {
                throw new ReviewConcurrencyError()
            }
        });
    }

    public mergePullRequest = (reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string): Promise<any> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/merge_request/merge`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json'
            },
            method: 'POST',
            body: JSON.stringify({
                shouldRemoveBranch,
                commitMessage
            })
        });
    }

    public getProjects = (): Promise<ProjectInfo[]> => {
        return fetch('/api/admin/projects', acceptJson)
            .then(r => r.json())
            .then(r => r as ProjectInfo[]);
    }

    public setupProjectHooks = (projectId: number): Promise<any> => {
        return fetch(`/api/admin/project/${projectId}/setup_hooks`, {
            ...acceptJson,
            method: 'POST'
        });
    }

    public getCurrentUser = (): Promise<UserState> => {
        return fetch(`/api/user/current`, acceptJson)
            .then(r => r.json())
            .then(r => r as UserState);
    }

    public getFilesToReview = (reviewId: ReviewId): Promise<FilesToReview> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/files`, acceptJson)
            .then(r => r.json())
            .then(r => r as FilesToReview);
    }
}
