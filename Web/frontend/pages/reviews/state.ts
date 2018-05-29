import actionCreatorFactory, { AnyAction } from "typescript-fsa";
import { Review } from "../../api/reviewer";

export interface ReviewsState {
    reviews: Review[];
}

const createAction = actionCreatorFactory('REVIEWS');
export const loadReviews = createAction('LOAD_REVIEWS');
export const reviewsLoaded = createAction<{reviews: Review[]}>('REVIEWS_LOADED');

const initial: ReviewsState = {
    reviews: []
}

export const reviewsReducer = (state: ReviewsState = initial, action: AnyAction): ReviewsState => {
    if (reviewsLoaded.match(action)) {
        return {
            ...state,
            reviews: action.payload.reviews
        };
    }

    return state;
}
