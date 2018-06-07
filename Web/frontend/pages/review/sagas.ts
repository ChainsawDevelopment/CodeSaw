import { takeEvery, call, take, actionChannel, put, select } from "redux-saga/effects";
import {
    selectCurrentRevisions,
    SelectCurrentRevisions,
    loadedRevisionsRangeInfo,
    selectFileForView,
    loadedFileDiff,
    loadReviewInfo,
    loadedReviewInfo,
    rememberRevision,
    RememberRevisionArgs,
    createGitLabLink,
    CreateGitLabLinkArgs,
    loadComments,
    loadedComments,
    addComment,
    AddCommentArgs
} from './state';
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi, ReviewInfo, ReviewId, RevisionRange, PathPair, Comment } from '../../api/reviewer';
import { RootState } from "../../rootState";

const resolveProvisional = (range: RevisionRange, hash: string): RevisionRange => {
    return {
        current: range.current == 'provisional' ? hash : range.current,
        previous: range.previous == 'provisional' ? hash : range.previous
    }
}

function* loadRevisionRangeDetailsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<SelectCurrentRevisions> = yield take(selectCurrentRevisions);

        const currentRange = yield select((state: RootState) => ({
            reviewId: state.review.currentReview.reviewId,
            range: state.review.range,
            headCommit: state.review.currentReview.headCommit
        }));

        const info = yield api.getRevisionRangeInfo(currentRange.reviewId, resolveProvisional(action.payload.range, currentRange.headCommit));

        yield put(loadedRevisionsRangeInfo(info));
    }
}

function* loadFileDiffSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ path: PathPair }> = yield take(selectFileForView);
        const currentRange = yield select((state: RootState) => ({
            reviewId: state.review.currentReview.reviewId,
            range: state.review.range,
            headCommit: state.review.currentReview.headCommit
        }));

        const diff = yield api.getDiff(currentRange.reviewId, resolveProvisional(currentRange.range, currentRange.headCommit), action.payload.path);

        yield put(loadedFileDiff(diff));
    }
}

function* loadReviewInfoSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ reviewId: ReviewId }> = yield take(loadReviewInfo);
        const info: ReviewInfo = yield api.getReviewInfo(action.payload.reviewId);
        yield put(loadedReviewInfo(info));
        yield put(selectCurrentRevisions({
            range: {
                previous: 'base',
                current: info.hasProvisionalRevision ? 'provisional' : info.pastRevisions[info.pastRevisions.length - 1]
            }
        }))
    }
}

function* rememberRevisionSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<RememberRevisionArgs> = yield take(rememberRevision);

        yield api.rememberRevision(action.payload.reviewId, action.payload.head, action.payload.base);

        yield put(loadReviewInfo({ reviewId: action.payload.reviewId }));
    }
}

function* createGitLabLinkSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<CreateGitLabLinkArgs> = yield take(createGitLabLink);

        yield api.createGitLabLink(action.payload.reviewId);
    }
}

function* loadCommentsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ reviewId: ReviewId }> = yield take(loadComments);
        const comments: Comment[] = yield api.getComments(action.payload.reviewId);
        yield put(loadedComments(comments));
    }
}

function* addCommentSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<AddCommentArgs> = yield take(addComment);

        yield api.addComment(action.payload.reviewId, action.payload.content, action.payload.parentId);

        yield put(loadComments({ reviewId: action.payload.reviewId }));
    }
}

export default [
    loadRevisionRangeDetailsSaga,
    loadFileDiffSaga,
    loadReviewInfoSaga,
    rememberRevisionSaga,
    createGitLabLinkSaga,
    loadCommentsSaga,
    addCommentSaga
];
