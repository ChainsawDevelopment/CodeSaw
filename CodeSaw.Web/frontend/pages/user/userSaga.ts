import { take, put } from "redux-saga/effects";
import { ReviewerApi } from '../../api/reviewer';
import { UserState } from "../../rootState";
import { loadCurrentUser, currentUserLoaded } from "./state";

function* loadCurrentUserSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        yield take(loadCurrentUser);
        const currentUser: UserState = yield api.getCurrentUser();

        yield put(currentUserLoaded(currentUser));
    }
}

export default [
    loadCurrentUserSaga
];