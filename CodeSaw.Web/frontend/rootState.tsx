import { ReviewState } from "./pages/review/state";
import { ReviewsState } from "./pages/reviews/state";
import { AdminState } from "./pages/admin/state";
import { LoadingState } from "./loading/state";

export interface UserState {
    userName: string;
    givenName: string;
    avatarUrl: string;
}

export interface RootState {
    review: ReviewState;
    reviews: ReviewsState;
    admin: AdminState;
    currentUser: UserState;
    loading: LoadingState;
}