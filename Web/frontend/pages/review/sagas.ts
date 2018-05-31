import { takeEvery, call, take, actionChannel, put, select } from "redux-saga/effects";
import { selectCurrentRevisions, RevisionRange, SelectCurrentRevisions, loadedRevisionsRangeInfo, selectFileForView, loadedFileDiff, loadReviewInfo, loadedReviewInfo } from './state';
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi, ReviewInfo } from '../../api/reviewer';
import { RootState } from "../../rootState";

function* loadRevisionRangeDetailsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<SelectCurrentRevisions> = yield take(selectCurrentRevisions);
        
        const currentRange = yield select((state: RootState) => ({
            projectId: state.review.currentReview.projectId,
            reviewId: state.review.currentReview.reviewId,
            range: state.review.range
        }));
        
        const info = yield api.getRevisionRangeInfo(currentRange.projectId, currentRange.reviewId, action.payload.range);
        
        yield put(loadedRevisionsRangeInfo(info));
    }
}

function* loadFileDiffSaga() {
    const api = new ReviewerApi();

    for(; ;) {
        const action: Action<{path: string}> = yield take(selectFileForView);
        const currentRange = yield select((state: RootState) => ({
            projectId: state.review.currentReview.projectId,
            reviewId: state.review.currentReview.reviewId,
            range: state.review.range
        }));

        const diff = yield api.getDiff(currentRange.projectId, currentRange.reviewId, currentRange.range, action.payload.path);
        
        yield put(loadedFileDiff(diff));
    }
}

function* loadReviewInfoSaga() {
    const api = new ReviewerApi();

    for(; ;) {
        const action: Action<{projectId: number, reviewId: number}> = yield take(loadReviewInfo);
        const info: ReviewInfo = yield api.getReviewInfo(action.payload.projectId, action.payload.reviewId);
        yield put(loadedReviewInfo(info));
        yield put(selectCurrentRevisions({
            range: {
                previous: 'base',
                current: info.hasProvisionalRevision ? 'provisional' : info.pastRevisions[info.pastRevisions.length - 1]
            }
        }))
    }
}

export default [
    loadRevisionRangeDetailsSaga,
    loadFileDiffSaga,
    loadReviewInfoSaga
];