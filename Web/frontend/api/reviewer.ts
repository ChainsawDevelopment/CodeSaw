export type RevisionId = 'base' | number | string | 'provisional';

export interface RevisionRange {
    previous: RevisionId;
    current: RevisionId;
}

export interface ChangedFile {
    newPath: string;
}

export interface RevisionRangeInfo {
    changes: ChangedFile[];
}

export interface DiffChunk {
    classification: 'Unchanged' | 'BaseChange' | 'ReviewChange';
    operation: 'Equal' | 'Insert' | 'Delete';
    text: string;
}

export interface FileDiff {
    chunks: DiffChunk[];
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

const acceptJson = {
    headers: {
        'Accept': 'application/json'
    }
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

    public getDiff = (reviewId: ReviewId, range: RevisionRange, path: string): Promise<FileDiff> => {
        return fetch(
            `/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/diff/${range.previous}/${range.current}?file=${path}`,
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
        const request = new Request(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/revision/remember`, acceptJson)

        return fetch(`/api/project/${reviewId.projectId}/review/${reviewId.reviewId}/revision/remember`, {
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
}