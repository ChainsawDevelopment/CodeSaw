import { take, put } from "redux-saga/effects";
import { Action } from "typescript-fsa";
import { ReviewerApi, Review, Paged, ReviewSearchArgs } from '../../api/reviewer';
import { loadReviews, reviewsLoaded } from "./state";
import { startOperation, stopOperation } from "../../loading/saga";

function* loadReviewsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<ReviewSearchArgs> = yield take(loadReviews);

        yield startOperation();

        const reviews: Paged<Review> = yield api.getReviews(action.payload);
        yield put(reviewsLoaded({ reviews }));
        
        yield stopOperation();
    }
}

export default [
    loadReviewsSaga
];