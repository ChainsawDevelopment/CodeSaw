import { takeEvery, call, take, actionChannel, put, select } from "redux-saga/effects";
import { selectCurrentRevisions, SelectCurrentRevisions, loadedRevisionsRangeInfo, selectFileForView, loadedFileDiff, loadReviewInfo, loadedReviewInfo, publishReview, ReviewState, createGitLabLink, CreateGitLabLinkArgs } from './state';
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi, ReviewInfo, ReviewId, RevisionRange, PathPair, ReviewSnapshot } from '../../api/reviewer';
import { RootState } from "../../rootState";

const resolveProvisional = (range: RevisionRange, hash: string): RevisionRange => {
    return {
        current: range.current == 'provisional' ? hash : range.current,
        previous: range.previous == 'provisional' ? hash : range.previous
    }
}

function* loadRevisionRangeDetailsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<SelectCurrentRevisions> = yield take(selectCurrentRevisions);

        const currentRange = yield select((state: RootState) => ({
            reviewId: state.review.currentReview.reviewId,
            range: state.review.range,
            headCommit: state.review.currentReview.headCommit
        }));

        const info = yield api.getRevisionRangeInfo(currentRange.reviewId, resolveProvisional(action.payload.range, currentRange.headCommit));

        yield put(loadedRevisionsRangeInfo(info));
    }
}

function* loadFileDiffSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ path: PathPair }> = yield take(selectFileForView);
        const currentRange = yield select((state: RootState) => ({
            reviewId: state.review.currentReview.reviewId,
            range: state.review.range,
            headCommit: state.review.currentReview.headCommit
        }));

        const diff = yield api.getDiff(currentRange.reviewId, resolveProvisional(currentRange.range, currentRange.headCommit), action.payload.path);

        yield put(loadedFileDiff(diff));
    }
}

function* loadReviewInfoSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ reviewId: ReviewId }> = yield take(loadReviewInfo);
        const info: ReviewInfo = yield api.getReviewInfo(action.payload.reviewId);

        const currentReview: ReviewId = yield select((s: RootState) => s.review.currentReview ? s.review.currentReview.reviewId : null);
        const currentRange: RevisionRange = yield select((s: RootState) => s.review.range);

        yield put(loadedReviewInfo(info));

        let newRange: RevisionRange = {
            previous: 'base',
            current: info.hasProvisionalRevision ? 'provisional' : info.pastRevisions[info.pastRevisions.length - 1].number
        }

        if (currentReview && action.payload.reviewId.projectId == currentReview.projectId && action.payload.reviewId.reviewId == currentReview.reviewId) {
            if (currentRange != null) {
                newRange = currentRange;
            }
        }

        yield put(selectCurrentRevisions({
            range: newRange
        }))
    }
}

function* createGitLabLinkSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<CreateGitLabLinkArgs> = yield take(createGitLabLink);

        yield api.createGitLabLink(action.payload.reviewId);
    }
}

function* publishReviewSaga() {
    const api = new ReviewerApi();
    for (; ;) {
        const action: Action<{}> = yield take(publishReview);
        const reviewSnapshot: ReviewSnapshot = yield select((s: RootState): ReviewSnapshot => ({
            reviewId: s.review.currentReview.reviewId,
            revision: s.review.rangeInfo.commits.current
        }));

        yield api.publishReview(reviewSnapshot);

        yield put(loadReviewInfo({ reviewId: reviewSnapshot.reviewId }));
    }
}

export default [
    loadRevisionRangeDetailsSaga,
    loadFileDiffSaga,
    loadReviewInfoSaga,
    createGitLabLinkSaga,
    publishReviewSaga,
];