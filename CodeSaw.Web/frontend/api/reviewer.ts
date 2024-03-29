import * as PathPairs from '../pathPair';
import { UserState } from '../rootState';
import { RemoteRevisionId, LocalRevisionId, RevisionId } from './revisionId';

export type FileId = string;

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
            base: string;
        };
        previous: {
            head: string;
            base: string;
        };
    };
    filesReviewedByUser: PathPairs.List;
}

export interface FileDiffRange {
    previous: {
        base: string;
        head: string;
    };
    current: {
        base: string;
        head: string;
    };
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
            previousTotalLines: number;
            current: string;
            currentTotalLines: number;
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

export type ReviewInfoState = 'opened' | 'reopened' | 'merged' | 'closed';

namespace remote {
    export interface Discussion {
        id: string;
        revision: RemoteRevisionId;
        state: CommentState;
        comment: Comment;
        canResolve: boolean;
    }

    export interface FileDiscussion extends Discussion {
        fileId: FileId;
        lineNumber: number;
    }

    export type ReviewDiscussion = Discussion;
}

export interface Discussion extends FilteredBase<remote.Discussion, 'revision'> {
    revision: LocalRevisionId;
}

export interface FileDiscussion extends Discussion, FilteredBase<remote.FileDiscussion, 'revision'> {
    fileId: FileId;
    lineNumber: number;
}

export interface ReviewDiscussion extends Discussion, FilteredBase<remote.ReviewDiscussion, 'revision'> {}

namespace remote {
    export interface FileToReview {
        fileId: FileId;
        reviewFile: PathPairs.PathPair;
        diffFile: PathPairs.PathPair;
        previous: RemoteRevisionId;
        current: RemoteRevisionId;
        changeType: 'modified' | 'renamed' | 'created' | 'deleted';
    }
}

type Diff<T, U> = T extends U ? never : T;

type FilteredBase<T, Remove> = {
    [K in Diff<keyof T, Remove>]: T[K];
};

export interface FileToReview extends FilteredBase<remote.FileToReview, 'previous' | 'current'> {
    previous: LocalRevisionId;
    current: LocalRevisionId;
}

export interface BuildStatus {
    status: 'success' | 'pending' | 'running' | 'failed' | 'canceled';
    name: string;
    targetUrl: string;
    description: string;
}

export type ReviewMergeStatus = 'can_be_merged' | 'cannot_be_merged' | 'unchecked';

export interface Commit {
    id: string;
    title: string;
    message: string;
    createdAt: Date;
    authorName: string;
    authorEmail: string;
}

namespace remote {
    export interface FileMatrixRevision {
        revision: RemoteRevisionId;
        file: PathPairs.PathPair;
        isNew: boolean;
        isRenamed: boolean;
        isDeleted: boolean;
        isUnchanged: boolean;
        reviewers: string[];
    }
    export interface FileMatrixEntry {
        file: PathPairs.PathPair;
        fileId: FileId;
        revisions: FileMatrixRevision[];
    }

    export interface ReviewInfo {
        reviewId: ReviewId;
        title: string;
        projectPath: string;
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
        headRevision: RemoteRevisionId;
        state: ReviewInfoState;
        mergeStatus: ReviewMergeStatus;
        fileDiscussions: FileDiscussion[];
        reviewDiscussions: ReviewDiscussion[];
        fileMatrix: FileMatrixEntry[];
        filesToReview: remote.FileToReview[];
        buildStatuses: BuildStatus[];
        sourceBranch: string;
        targetBranch: string;
        reviewFinished: boolean;
        author: UserState;
        isAuthor: boolean;
        commits: Commit[];
    }
}

export interface FileMatrixRevision extends FilteredBase<remote.FileMatrixRevision, 'revision'> {
    revision: LocalRevisionId; // TODO: can be RevisionSelected
}

export interface FileMatrixEntry extends FilteredBase<remote.FileMatrixEntry, 'revisions'> {
    revisions: FileMatrixRevision[];
}

export interface ReviewInfo
    extends FilteredBase<
        remote.ReviewInfo,
        'filesToReview' | 'fileDiscussions' | 'reviewDiscussions' | 'headRevision' | 'fileMatrix'
    > {
    headRevision: LocalRevisionId;
    filesToReview: FileToReview[];
    fileDiscussions: FileDiscussion[];
    reviewDiscussions: ReviewDiscussion[];
    fileMatrix: FileMatrixEntry[];
}

export interface CommentReply {
    id: string;
    parentId: string;
    content: string;
}

export interface ReviewSnapshotFileRef {
    revision: RemoteRevisionId;
    fileId: FileId;
}

export interface ReviewSnapshot {
    reviewId: ReviewId;
    revision: {
        head: string;
        base: string;
    };
    startedFileDiscussions: {
        targetRevisionId: RemoteRevisionId;
        temporaryId: string;
        fileId: FileId;
        lineNumber: number;
        content: string;
        state: CommentState;
    }[];
    startedReviewDiscussions: {
        targetRevisionId: RemoteRevisionId;
        temporaryId: string;
        content: string;
        state: CommentState;
    }[];
    resolvedDiscussions: string[];
    replies: CommentReply[];
    reviewedFiles: ReviewSnapshotFileRef[];
    unreviewedFiles: ReviewSnapshotFileRef[];
}

export type CommentState = 'NoActionNeeded' | 'NeedsResolution' | 'Resolved' | 'ResolvePending' | 'GoodWork';

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
        Accept: 'application/json',
    },
    credentials: 'include' as RequestCredentials,
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
};

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
    orderBy: 'created_at' | 'updated_at';
    sort: 'asc' | 'desc';
    nameFilter: string;
    page: number;
    state: string;
}

export interface RemappedDiffDiscussion {
    discussionId: string;
    lineNumber: number;
    side: 'left' | 'right';
}

export interface DiffDiscussions {
    remapped: RemappedDiffDiscussion[];
}

export class ReviewerApi {
    public getDiff = (reviewId: ReviewId, range: FileDiffRange, path: PathPairs.PathPair): Promise<FileDiff> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/diff/${range.previous.base}/${range.previous.head}/${range.current.base}/${range.current.head}?oldPath=${path.oldPath}&newPath=${path.newPath}`,
            acceptJson,
        )
            .then(mustBeOk)
            .then((r) => r.json());
    };

    public getDiffDiscussions = (
        reviewId: ReviewId,
        range: FileDiffRange,
        fileId: FileId,
        rightFileName: string,
    ): Promise<DiffDiscussions> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/discussions/${range.previous.base}/${range.previous.head}/${range.current.base}/${range.current.head}/${fileId}?fileName=${rightFileName}`,
            acceptJson,
        )
            .then(mustBeOk)
            .then((r) => r.json());
    };

    public getReviews = (args: ReviewSearchArgs): Promise<Paged<Review>> => {
        return fetch(
            `/api/reviews?orderBy=${args.orderBy}&sort=${args.sort}&search=${args.nameFilter}&page=${args.page}&state=${args.state}`,
            acceptJson,
        )
            .then(mustBeOk)
            .then((r) => r.json())
            .then((r) => r as Paged<Review>);
    };

    public getReviewInfo = (reviewId: ReviewId): Promise<ReviewInfo> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/info`, acceptJson)
            .then(mustBeOk)
            .then((r) => r.json())
            .then((r) => r as remote.ReviewInfo)
            .then((r) => ({
                ...r,
                headRevision: RevisionId.mapRemoteToLocal(r.headRevision),
                filesToReview: r.filesToReview.map((f) => ({
                    ...f,
                    previous: RevisionId.mapRemoteToLocal(f.previous),
                    current: RevisionId.mapRemoteToLocal(f.current),
                })),
                fileDiscussions: r.fileDiscussions.map((d) => ({
                    ...d,
                    revision: RevisionId.mapRemoteToLocal(d.revision),
                })),
                reviewDiscussions: r.reviewDiscussions.map((d) => ({
                    ...d,
                    revision: RevisionId.mapRemoteToLocal(d.revision),
                })),
                fileMatrix: r.fileMatrix.map((e) => ({
                    ...e,
                    revisions: e.revisions.map((r) => ({
                        ...r,
                        revision: RevisionId.mapRemoteToLocal(r.revision),
                    })),
                })),
            }));
    };

    public publishReview = (review: ReviewSnapshot): Promise<void> => {
        const { reviewId, ...snapshot } = review;

        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/publish`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json',
            },
            method: 'POST',
            body: JSON.stringify(snapshot),
        }).then((r) => {
            if (r.status == 409) {
                throw new ReviewConcurrencyError();
            } else {
                mustBeOk(r);
            }
        });
    };

    public mergePullRequest = (
        reviewId: ReviewId,
        shouldRemoveBranch: boolean,
        commitMessage: string,
    ): Promise<any> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/merge_request/merge`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json',
            },
            method: 'POST',
            body: JSON.stringify({
                shouldRemoveBranch,
                commitMessage,
            }),
        })
            .then((r) => {
                if (r.status == 418) {
                    throw new MergeFailedError();
                }

                return r;
            })
            .then(mustBeOk);
    };

    public getProjects = (): Promise<ProjectInfo[]> => {
        return fetch('/api/admin/projects', acceptJson)
            .then(mustBeOk)
            .then((r) => r.json())
            .then((r) => r as ProjectInfo[]);
    };

    public setupProjectHooks = (projectId: number): Promise<any> => {
        return fetch(`/api/admin/project/${projectId}/setup_hooks`, {
            ...acceptJson,
            method: 'POST',
        }).then(mustBeOk);
    };

    public getCurrentUser = (): Promise<UserState> => {
        return fetch(`/api/user/current`, acceptJson)
            .then(mustBeOk)
            .then((r) => r.json())
            .then((r) => r as UserState);
    };
}
