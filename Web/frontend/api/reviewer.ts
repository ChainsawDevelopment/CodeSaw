export type RevisionId = 'base' | number | string | 'provisional';

export interface RevisionRange {
    previous: RevisionId;
    current: RevisionId;
}

export interface PathPair {
    newPath: string;
    oldPath: string;
}

export const emptyPathPair: PathPair = { newPath: null, oldPath: null };

export interface ChangedFile {
    path: PathPair;
    renamedFile: boolean;
}

export interface RevisionRangeInfo {
    changes: ChangedFile[];
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

export interface ReviewInfo {
    reviewId: ReviewId;
    title: string;
    pastRevisions: RevisionId[];
    hasProvisionalRevision: boolean;
    headCommit: string;
    baseCommit: string;
}

export interface ReviewSnapshot {
    reviewId: ReviewId;
    revision: {
        head: string,
        base: string
    }
}

const acceptJson = {
    headers: {
        'Accept': 'application/json'
    },
    credentials: 'include' as RequestCredentials
};

export class ReviewerApi {
    public getRevisionRangeInfo = (reviewId: ReviewId, range: RevisionRange): Promise<RevisionRangeInfo> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/revisions/${range.previous}/${range.current}`,
            acceptJson
        )
            .then(r => r.json())
            .then(r => r as RevisionRangeInfo);
    }

    public getDiff = (reviewId: ReviewId, range: RevisionRange, path: PathPair): Promise<FileDiff> => {
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
    }

    public rememberRevision = (reviewId: ReviewId, head: string, base: string): Promise<any> => {
        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/revision/remember`, {
            ...acceptJson,
            headers: {
                ...acceptJson.headers,
                'Content-Type': 'application/json'
            },
            method: 'POST',
            body: JSON.stringify({
                headCommit: head,
                baseCommit: base
            })
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

    public publishReview = (review: ReviewSnapshot): Promise<{}> => {
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
        );
    }
}