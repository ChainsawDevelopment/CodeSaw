import { ReviewState, UnpublishedReview } from './state'
import { ReviewId } from '@api/reviewer'

const createReviewKey = (reviewId: ReviewId): string => {
    return `unpublished_${reviewId.projectId}_${reviewId.reviewId}`;
}

const emptyUnpublishedReview: UnpublishedReview = {
    unpublishedFileDiscussions: [],
    unpublishedReplies: [],
    unpublishedResolvedDiscussions: [],
    unpublishedReviewDiscussions: [],
    unpublishedReviewedFiles: {},
    unpublishedUnreviewedFiles: {}
};

export const saveUnpublishedReview = (review: ReviewState): void => {
    const reviewKey = createReviewKey(review.currentReview.reviewId);
    
    const unpublishedData = {};
    Object.keys(review).filter(key => key.startsWith("unpublished")).forEach(key => {
        unpublishedData[key] = review[key];
    });

    try {
        localStorage.setItem(reviewKey, JSON.stringify(unpublishedData));
    }
    catch (err) {
        console.warn({msg: "Error when writing to local storage", err});
    }
    
}

export const clearUnpublishedReview = (reviewId: ReviewId): void => {
    const reviewKey = createReviewKey(reviewId);
    try {
        localStorage.setItem(reviewKey, JSON.stringify({}));
    }
    catch (err) {
        console.warn({msg: "Error when clearing local storage", err});
    }
}

export const getUnpublishedReview = (reviewId: ReviewId): UnpublishedReview => {
    const reviewKey = createReviewKey(reviewId);

    try {
        const serializedData = localStorage.getItem(reviewKey);
        if (serializedData === null) {
            return emptyUnpublishedReview;
        }
        return JSON.parse(serializedData) as UnpublishedReview;
    } catch(err) {
        console.warn({msg: "Error when reading from local storage", err});
        return emptyUnpublishedReview;
    }
}