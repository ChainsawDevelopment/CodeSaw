import { takeEvery, call, take, actionChannel, put, select } from "redux-saga/effects";
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi, Review } from '../../api/reviewer';
import { RootState } from "../../rootState";
import { loadReviews, reviewsLoaded } from "./state";

function* loadReviewsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{}> = yield take(loadReviews);
        const reviews: Review[] = yield api.getReviews();

        yield put(reviewsLoaded({ reviews }));
    }
}

export default [
    loadReviewsSaga
];