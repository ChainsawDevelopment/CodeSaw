import { RevisionRange } from "../pages/review/state";

export interface ChangedFile {
    path: string;
}

export interface RevisionRangeInfo {
    changes: ChangedFile[];
}

export interface DiffChunk {
    classification: 'unchanged' | 'base' | 'review';
    operation: 'equal' | 'insert' | 'delete';
    text: string;
}

export interface FileDiff {
    chunks: DiffChunk[];
}

export interface Review {
    reviewId: number;
    project: string;
    title: string;
    changesCount: number;
    author: string;
}

const acceptJson = {
    headers: {
        'Accept': 'application/json'
    }
};

export class ReviewerApi {
    public getRevisionRangeInfo = (reviewId: number, range: RevisionRange): Promise<RevisionRangeInfo> => {
        return fetch(
            `/api/review/${reviewId}/revisions/${range.previous}/${range.current}`,
            acceptJson
        )
            .then(r => r.json())
            .then(r => r as RevisionRangeInfo);
    }

    public getDiff = (reviewId: number, range: RevisionRange, path: string): Promise<FileDiff> => {
        return fetch(
            `/api/review/${reviewId}/diff/${range.previous}/${range.current}/${path}`,
            acceptJson
        ).then(r => r.json());
    };

    public getReviews = (): Promise<Review[]> => {
        return fetch('/api/reviews', acceptJson)
            .then(r => r.json())
            .then(r => r as Review[]);
    };
}