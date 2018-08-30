import actionCreatorFactory, { AnyAction } from "typescript-fsa";
import { Review, Paged, ReviewSearchArgs } from "../../api/reviewer";

export interface ReviewsState {
    reviews: Paged<Review>;
}

const createAction = actionCreatorFactory('REVIEWS');
export const loadReviews = createAction<ReviewSearchArgs>('LOAD_REVIEWS');
export const reviewsLoaded = createAction<{reviews: Paged<Review>}>('REVIEWS_LOADED');

const initial: ReviewsState = {
    reviews: {
        items: [],
        page: 1,
        perPage: 20,
        totalItems: 0,
        totalPages: 1
    }
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
