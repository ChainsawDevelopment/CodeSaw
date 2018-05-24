import { takeEvery, call, take, actionChannel, put, select } from "redux-saga/effects";
import { selectCurrentRevisions, RevisionRange, SelectCurrentRevisions, loadedRevisionsRangeInfo, selectFileForView, loadedFileDiff } from './state';
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi } from '../../api/reviewer';
import { RootState } from "../../rootState";

function* loadRevisionRangeDetailsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<SelectCurrentRevisions> = yield take(selectCurrentRevisions);
        
        const info = yield api.getRevisionRangeInfo(4, action.payload.range);
        
        yield put(loadedRevisionsRangeInfo(info));
    }
}

function* loadFileDiffSaga() {
    const api = new ReviewerApi();

    for(; ;) {
        const action: Action<{path: string}> = yield take(selectFileForView);
        const currentRange = yield select((state: RootState) => ({
            reviewId: 4, //state.review.id
            range: state.review.range
        }));

        const diff = yield api.getDiff(currentRange.reviewId, currentRange.range, action.payload.path);
        
        yield put(loadedFileDiff(diff));
    }
}

export default [
    loadRevisionRangeDetailsSaga,
    loadFileDiffSaga
];