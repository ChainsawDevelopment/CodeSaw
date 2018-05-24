import { takeEvery, call, take, actionChannel, put } from "redux-saga/effects";
import { selectCurrentRevisions, RevisionRange, SelectCurrentRevisions, loadedRevisionsRangeInfo } from './state';
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi } from '../../api/reviewer';

function* loadRevisionRangeDetailsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<SelectCurrentRevisions> = yield take(selectCurrentRevisions);
        
        const info = yield api.getRevisionRangeInfo(4, action.payload.range);
        
        yield put(loadedRevisionsRangeInfo(info));
    }
}

export default [
    loadRevisionRangeDetailsSaga
];