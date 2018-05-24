import { RevisionRange } from "../pages/review/state";

export interface ChangedFile {
    path: string;
}

export interface RevisionRangeInfo {
    changes: ChangedFile[];
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
}