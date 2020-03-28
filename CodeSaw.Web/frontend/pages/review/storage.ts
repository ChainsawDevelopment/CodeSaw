import { ReviewState, UnpublishedReview, emptyUnpublishedReview } from './state'
import { ReviewId } from '@api/reviewer'

const createReviewKey = (reviewId: ReviewId): string => {
    return `unpublished_${reviewId.projectId}_${reviewId.reviewId}`;
}

export const saveUnpublishedReview = (review: ReviewState): void => {
    const reviewKey = createReviewKey(review.currentReview.reviewId);

    const unpublishedData = {};
    Object.keys(review).filter(key => key.startsWith("unpublished")).forEach(key => {
        unpublishedData[key] = review[key];
    });

    unpublishedData['nextReplyId'] = review.nextReplyId;
    unpublishedData['nextDiscussionCommentId'] = review.nextDiscussionCommentId;

    try {
        localStorage.setItem(reviewKey, JSON.stringify(unpublishedData));
    }
    catch (err) {
        console.warn({msg: "Error when writing to local storage", err});
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

export const getReviewVSCodeWorkspace = (reviewId: ReviewId) : string => {
    const key = `vscodeworkspace_${reviewId.projectId}`

    try {
        return localStorage.getItem(key);
    } catch(err) {
        return null;
    }
}

export const saveReviewVSCodeWorkspace = (reviewId: ReviewId, vsCodeWorkspace: string) : void => {
    const key = `vscodeworkspace_${reviewId.projectId}`

    try {
        localStorage.setItem(key, vsCodeWorkspace);
    }
    catch (err) {
        console.warn({msg: "Error when writing vsCodeWorkspace to local storage", err});
    }
    
}