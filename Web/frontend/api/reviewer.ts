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
    contents: {
        base: {
            previous: string;
            current: string;
        };
        review: {
            previous: string;
            current: string;
        };
    };
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
    author: ReviewAuthor;
    sourceBranch: string;
    targetBranch: string;
    isCreatedByMe: boolean;
    amIReviewer: boolean;
}

export type ReviewInfoState = 'opened' | "reopened" | "merged" | "closed";

export interface Discussion 
{
    id: string;
    revision: RevisionId;
    state: CommentState;
    comment: Comment;
    canResolve: boolean;
}

export interface FileDiscussion extends Discussion
{
    filePath: PathPairs.PathPair;
    lineNumber: number;
}

export interface ReviewDiscussion extends Discussion
{
}

export interface FileToReview {
    reviewFile: PathPairs.PathPair;
    diffFile: PathPairs.PathPair;
    previous: RevisionId;
    current: RevisionId;
    changeType: 'modified' | 'renamed' | 'created' | 'deleted';
}

export interface BuildStatus {
    status: 'success' | 'pending' | 'running' | 'failed' | 'canceled';
    name: string;
    targetUrl: string;
    description: string;
}

export type ReviewMergeStatus = 'can_be_merged' | 'cannot_be_merged' | 'unchecked';

export interface ReviewInfo {
    reviewId: ReviewId;
    title: string;
    description: string;
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
    mergeStatus: ReviewMergeStatus;
    fileDiscussions: FileDiscussion[];
    reviewDiscussions: ReviewDiscussion[];
    fileMatrix: any;
    filesToReview: FileToReview[];
    buildStatuses: BuildStatus[];
    sourceBranch: string;
    targetBranch: string;
    reviewFinished: boolean;
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
    startedFileDiscussions: {
        targetRevisionId: RevisionId;
        temporaryId: string;
        file: PathPairs.PathPair;
        lineNumber: number;
        content: string;
        needsResolution: boolean;
    }[];
    startedReviewDiscussions: {
        targetRevisionId: RevisionId;
        temporaryId: string;
        needsResolution: boolean;
        content: string;
    }[];
    resolvedDiscussions: string[]; 
    replies: CommentReply[];
    reviewedFiles: {
        [revision: string]: PathPairs.List;
    };
    unreviewedFiles: {
        [revision: string]: PathPairs.List;
    };
}

export type CommentState = 'NoActionNeeded' | 'NeedsResolution' | 'Resolved' | 'ResolvePending';

export interface Comment {
    id: string;
    author: UserState;
    content: string;
    createdAt: string;
    children: Comment[];
}

export interface ProjectInfo {
    id: number;
    namespace: string;
    name: string;
    canConfigureHooks: boolean;
}

export interface PageInfo {
    perPage: number;
    page: number;
    totalPages: number;
    totalItems: number;
}

export interface Paged<T> extends PageInfo {
    items: T[];
}

const acceptJson = {
    headers: {
        'Accept': 'application/json'
    },
    credentials: 'include' as RequestCredentials
};

export class ReviewerApiError extends Error {
    constructor(statusCode: number, url: string) {
        super(`Reviewer API error: HTTP ${statusCode} at '${url}'`);
    }
}

const mustBeOk = (value: Response): Response => {
    if (!value.ok) {
        throw new ReviewerApiError(value.status, value.url);
    }
    return value;
}

export class ReviewConcurrencyError extends Error {
    constructor() {
        super('Publish review concurrency issue');
    }
}

export class MergeFailedError extends Error {
    constructor() {
        super('Merge failed');
    }
}

export interface ReviewSearchArgs {
    page: number;
    state: string;
}

export class ReviewerApi {
    public getDiff = (reviewId: ReviewId, range: RevisionRange, path: PathPairs.PathPair): Promise<FileDiff> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/diff/${range.previous}/${range.current}?oldPath=${path.oldPath}&newPath=${path.newPath}`,
            acceptJson
        ).then(mustBeOk).then(r => r.json());
    };

    public getReviews = (args: ReviewSearchArgs): Promise<Paged<Review>> => {
        return fetch(`/api/reviews?page=${args.page}&state=${args.state}`, acceptJson)
            .then(mustBeOk)
            .then(r => r.json())
            .then(r => r as Paged<Review>);
    };

    public getReviewInfo = (reviewId: ReviewId): Promise<ReviewInfo> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/info`, acceptJson)
            .then(mustBeOk)
            .then(r => r.json())
            .then(r => r as ReviewInfo);
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
            } else {
                mustBeOk(r);
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
        })
        .then(r => {
            if (r.status == 418) {
                throw new MergeFailedError();
            }

            return r;
        })
        .then(mustBeOk);
    }

    public getProjects = (): Promise<ProjectInfo[]> => {
        return fetch('/api/admin/projects', acceptJson)
            .then(mustBeOk)
            .then(r => r.json())
            .then(r => r as ProjectInfo[]);
    }

    public setupProjectHooks = (projectId: number): Promise<any> => {
        return fetch(`/api/admin/project/${projectId}/setup_hooks`, {
            ...acceptJson,
            method: 'POST'
        }).then(mustBeOk);
    }

    public getCurrentUser = (): Promise<UserState> => {
        return fetch(`/api/user/current`, acceptJson)
            .then(mustBeOk)
            .then(r => r.json())
            .then(r => r as UserState);
    }
}
