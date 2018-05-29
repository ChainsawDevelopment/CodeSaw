import { ReviewState } from "./pages/review/state";
import { ReviewsState } from "./pages/reviews/state";

export interface RootState {
    review: ReviewState;
    reviews: ReviewsState;
}