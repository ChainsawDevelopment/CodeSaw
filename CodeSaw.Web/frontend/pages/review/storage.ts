import { ReviewState, UnpublishedReview, emptyUnpublishedReview } from './state';
import { ReviewId } from '@api/reviewer';

const STORAGE_VERSION = '1';

const createReviewKey = (reviewId: ReviewId): string => {
    return `unpublished_${reviewId.projectId}_${reviewId.reviewId}_${STORAGE_VERSION}`;
};

export interface LocallyStoredReview {
    unpublished: UnpublishedReview;
    fileIdMap: { [fileId: string]: string };
}

export const saveUnpublishedReview = (review: ReviewState): void => {
    const reviewKey = createReviewKey(review.currentReview.reviewId);

    const fileIdMap = {};
    for (const file of review.currentReview.filesToReview) {
        fileIdMap[file.fileId] = file.reviewFile.newPath;
    }

    const unpublishedData: LocallyStoredReview = {
        unpublished: {
            baseCommit: review.baseCommit,
            headCommit: review.headCommit,
            nextReplyId: review.nextReplyId,
            nextDiscussionCommentId: review.nextDiscussionCommentId,
            unpublishedFileDiscussions: review.unpublishedFileDiscussions,
            unpublishedReviewDiscussions: review.unpublishedReviewDiscussions,
            unpublishedReplies: review.unpublishedReplies,
            unpublishedResolvedDiscussions: review.unpublishedResolvedDiscussions,
            unpublishedReviewedFiles: review.unpublishedReviewedFiles,
            unpublishedUnreviewedFiles: review.unpublishedUnreviewedFiles,
        },
        fileIdMap: fileIdMap,
    };

    try {
        localStorage.setItem(reviewKey, JSON.stringify(unpublishedData));
    } catch (err) {
        console.warn({ msg: 'Error when writing to local storage', err });
    }
};

export const getUnpublishedReview = (reviewId: ReviewId): LocallyStoredReview => {
    const reviewKey = createReviewKey(reviewId);

    try {
        const serializedData = localStorage.getItem(reviewKey);
        if (serializedData === null) {
            return {
                unpublished: emptyUnpublishedReview,
                fileIdMap: {},
            };
        }
        return JSON.parse(serializedData) as LocallyStoredReview;
    } catch (err) {
        console.warn({ msg: 'Error when reading from local storage', err });
        return {
            unpublished: emptyUnpublishedReview,
            fileIdMap: {},
        };
    }
};

export const getReviewVSCodeWorkspace = (reviewId: ReviewId): string => {
    const key = `vscodeworkspace_${reviewId.projectId}`;

    try {
        return localStorage.getItem(key);
    } catch (err) {
        return null;
    }
};

export const saveReviewVSCodeWorkspace = (reviewId: ReviewId, vsCodeWorkspace: string): void => {
    const key = `vscodeworkspace_${reviewId.projectId}`;

    try {
        localStorage.setItem(key, vsCodeWorkspace);
    } catch (err) {
        console.warn({ msg: 'Error when writing vsCodeWorkspace to local storage', err });
    }
};
