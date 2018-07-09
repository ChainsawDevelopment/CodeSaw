import { takeEvery, call, take, actionChannel, put, select } from "redux-saga/effects";
import {
    selectCurrentRevisions,
    SelectCurrentRevisions,
    loadedRevisionsRangeInfo,
    selectFileForView,
    loadedFileDiff,
    loadReviewInfo,
    loadedReviewInfo,
    publishReview,
    createGitLabLink,
    CreateGitLabLinkArgs,
    loadComments,
    loadedComments,
    mergePullRequest,
    MergePullRequestArgs
} from './state';
import { Action, ActionCreator } from "typescript-fsa";
import { ReviewerApi, ReviewInfo, ReviewId, RevisionRange, ReviewSnapshot, ReviewConcurrencyError, Comment, RevisionRangeInfo } from '../../api/reviewer';
import { RootState } from "../../rootState";
import { delay } from "redux-saga";
import * as PathPairs from '../../pathPair';

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
        const action: Action<{ path: PathPairs.PathPair }> = yield take(selectFileForView);
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
        const action: Action<{ reviewId: ReviewId, fileToPreload?: string }> = yield take(loadReviewInfo);
        const info: ReviewInfo = yield api.getReviewInfo(action.payload.reviewId);

        const currentReview: ReviewId = yield select((s: RootState) => s.review.currentReview ? s.review.currentReview.reviewId : null);
        const currentRange: RevisionRange = yield select((s: RootState) => s.review.range);

        yield put(loadedReviewInfo(info));
        yield put(loadComments({}));

        let newRange: RevisionRange = {
            previous: 'base',
            current: info.hasProvisionalRevision ? 'provisional' : info.pastRevisions[info.pastRevisions.length - 1].number
        }

        if (currentReview && action.payload.reviewId.projectId == currentReview.projectId && action.payload.reviewId.reviewId == currentReview.reviewId) {
            if (currentRange != null) {
                newRange = currentRange;
            }

            if(newRange.current == 'provisional' && !info.hasProvisionalRevision) {
                newRange.current = info.pastRevisions[info.pastRevisions.length - 1].number;
            }
        }

        yield put(selectCurrentRevisions({
            range: newRange
        }))

        if (action.payload.fileToPreload) {
            const rangeInfo: Action<RevisionRangeInfo> = yield take(loadedRevisionsRangeInfo);

            const fullPath = rangeInfo.payload.changes.find(f => f.path.newPath == action.payload.fileToPreload).path;

            yield put(selectFileForView({path: fullPath}));
        }
    }
}

function* createGitLabLinkSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<CreateGitLabLinkArgs> = yield take(createGitLabLink);

        yield api.createGitLabLink(action.payload.reviewId);
    }
}

function* publishReviewSaga() {
    const api = new ReviewerApi();
    for (; ;) {
        const action: Action<{}> = yield take(publishReview);
        const reviewSnapshot: ReviewSnapshot = yield select((s: RootState): ReviewSnapshot => ({
            reviewId: s.review.currentReview.reviewId,
            revision: s.review.rangeInfo.commits.current,
            previous: s.review.rangeInfo.commits.previous,
            reviewedFiles: s.review.reviewedFiles,
            comments: s.review.comments
        }));

        for (let i = 0; i < 100; i++) {
            try {
                yield api.publishReview(reviewSnapshot);
                break;
            } catch(e) {
                if(!(e instanceof ReviewConcurrencyError)) {
                    throw e;
                }
            }
            console.log('Review publish failed due to concurrency issue. Retrying attempt ', i);
            yield delay(5000);
        }

        yield put(loadReviewInfo({ reviewId: reviewSnapshot.reviewId }));
    }
}

function* loadCommentsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        yield take(loadComments);

        const currentReview: ReviewId = yield select((s: RootState) => s.review.currentReview ? s.review.currentReview.reviewId : null);
        const comments: Comment[] = yield api.getComments(currentReview);

        yield put(loadedComments(comments));
    }
}

function* mergePullRequestSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<MergePullRequestArgs> = yield take(mergePullRequest);

        yield api.mergePullRequest(action.payload.reviewId, action.payload.shouldRemoveBranch, action.payload.commitMessage);

        yield put(loadReviewInfo({ reviewId: action.payload.reviewId }));
    }
}

export default [
    loadRevisionRangeDetailsSaga,
    loadFileDiffSaga,
    loadReviewInfoSaga,
    createGitLabLinkSaga,
    publishReviewSaga,
    mergePullRequestSaga,
    loadCommentsSaga
];
