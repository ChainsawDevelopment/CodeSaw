import  * as PathPairs from "../pathPair";

export type RevisionId = 'base' | number | string | 'provisional';

export interface RevisionRange {
    previous: RevisionId;
    current: RevisionId;
}

export interface ChangedFile {
    path: PathPairs.PathPair;
    renamedFile: boolean;
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

export interface FileDiff {
    hunks: Hunk[];
}

export interface ReviewId {
    projectId: number;
    reviewId: number;
}

export interface Review {
    reviewId: ReviewId;
    project: string;
    title: string;
    changesCount: number;
    author: string;
}

export type ReviewInfoState = 'opened' | "reopened" | "merged" | "closed";

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
    state: ReviewInfoState;
    mergeStatus: 'can_be_merged' | 'cannot_be_merged' | 'unchecked';
    reviewSummary: {
        file: string;
        revisions: {
            [revision: number]: string[];
        }
    }[];
}

export interface ReviewSnapshot {
    reviewId: ReviewId;
    revision: {
        head: string,
        base: string
    };
    previous: {
        head: string;
        base: string;
    }
    reviewedFiles: PathPairs.PathPair[];
}

export type CommentState = 'NoActionNeeded' | 'NeedsResolution' | 'Resolved';

export interface Comment {
    id: string;
    author: string;
    content: string;
    state: CommentState,
    createdAt: string;
    children: Comment[];
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
    public getRevisionRangeInfo = (reviewId: ReviewId, range: RevisionRange): Promise<RevisionRangeInfo> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/revisions/${range.previous}/${range.current}`,
            acceptJson
        )
            .then(r => r.json())
            .then(r => r as RevisionRangeInfo);
    }

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
            .then(r => r as ReviewInfo)
            .then(ri => {
                for(let item of ri.reviewSummary) {
                    const converted = {};

                    for (let rev of Object.keys(item.revisions)) {
                        converted[parseInt(rev)] = item.revisions[rev];
                    }
                    
                    item.revisions = converted;
                }

                return ri;
            });
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

    public getComments = (reviewId: ReviewId): Promise<Comment[]> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/comments`, acceptJson)
            .then(r => r.json())
            .then(r => r as Comment[]);
    }

    public addComment = (reviewId: ReviewId, content: string, needsResolution: boolean, parentId?: string): Promise<any> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/comment/add`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json'
            },
            method: 'POST',
            body: JSON.stringify({
                parentId,
                content,
                needsResolution
            })
        });
    }

    public resolveComment = (reviewId: ReviewId, commentId: string): Promise<any> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/comment/resolve`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json'
            },
            method: 'POST',
            body: JSON.stringify({
                commentId
            })
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
}
