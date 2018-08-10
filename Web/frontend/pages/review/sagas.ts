import { take, put, select } from "redux-saga/effects";
import {
    selectFileForView,
    loadedFileDiff,
    loadReviewInfo,
    loadedReviewInfo,
    publishReview,
    createGitLabLink,
    CreateGitLabLinkArgs,
    mergePullRequest,
    MergePullRequestArgs,
    PublishReviewArgs,
} from './state';
import { Action } from "typescript-fsa";
import { ReviewerApi, ReviewInfo, ReviewId, RevisionRange, ReviewSnapshot, ReviewConcurrencyError } from '../../api/reviewer';
import { RootState } from "../../rootState";
import { delay } from "redux-saga";
import * as PathPairs from '../../pathPair';

const resolveProvisional = (range: RevisionRange, hash: string): RevisionRange => {
    return {
        current: range.current == 'provisional' ? hash : range.current,
        previous: range.previous == 'provisional' ? hash : range.previous
    }
}

function* loadFileDiffSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ path: PathPairs.PathPair }> = yield take(selectFileForView);
        const currentRange = yield select((state: RootState) => ({
            reviewId: state.review.currentReview.reviewId,
            range: {
                previous: state.review.selectedFile.fileToReview.previous,
                current: state.review.selectedFile.fileToReview.current,
            },
            headCommit: state.review.currentReview.headCommit,
            path: state.review.selectedFile.fileToReview.diffFile
        }));

        const diff = yield api.getDiff(currentRange.reviewId, resolveProvisional(currentRange.range, currentRange.headCommit), currentRange.path);

        yield put(loadedFileDiff(diff));
    }
}

function* loadReviewInfoSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ reviewId: ReviewId, fileToPreload?: string }> = yield take(loadReviewInfo);
        const info: ReviewInfo = yield api.getReviewInfo(action.payload.reviewId);

        const currentReview: ReviewId = yield select((s: RootState) => s.review.currentReview ? s.review.currentReview.reviewId : null);

        yield put(loadedReviewInfo(info));

        let newRange: RevisionRange = {
            previous: 'base',
            current: info.hasProvisionalRevision ? 'provisional' : info.pastRevisions[info.pastRevisions.length - 1].number
        }

        if (currentReview && action.payload.reviewId.projectId == currentReview.projectId && action.payload.reviewId.reviewId == currentReview.reviewId) {
            if (newRange.current == 'provisional' && !info.hasProvisionalRevision) {
                newRange.current = info.pastRevisions[info.pastRevisions.length - 1].number;
            }
        }

        if (action.payload.fileToPreload) {
            const file = info.filesToReview.find(f => f.reviewFile.newPath == action.payload.fileToPreload)
            if (file != null) {
                yield put(selectFileForView({ path: file.reviewFile }));
            }
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
        const action: Action<PublishReviewArgs> = yield take(publishReview);
        const reviewSnapshot: ReviewSnapshot = yield select((s: RootState): ReviewSnapshot => ({
            reviewId: s.review.currentReview.reviewId,
            revision: {
                base: s.review.currentReview.baseCommit,
                head: s.review.currentReview.headCommit
            },
            startedFileDiscussions: s.review.unpublishedFileDiscussions.map(d => ({
                targetRevisionId: d.revision,
                temporaryId: d.comment.id,
                file: d.filePath,
                lineNumber: d.lineNumber,
                needsResolution: d.comment.state == 'NeedsResolution',
                content: d.comment.content
            })),
            startedReviewDiscussions: s.review.unpublishedReviewDiscussions.map(d => ({
                targetRevisionId: d.revision,
                temporaryId: d.comment.id,
                content: d.comment.content,
                needsResolution: d.comment.state == 'NeedsResolution'
            })),
            resolvedDiscussions: s.review.unpublishedResolvedDiscussions,
            replies: s.review.unpublishedReplies,
            reviewedFiles: s.review.unpublishedReviewedFiles,
            unreviewedFiles: s.review.unpublishedUnreviewedFiles
        }));

        for (let i = 0; i < 100; i++) {
            try {
                yield api.publishReview(reviewSnapshot);
                break;
            } catch (e) {
                if (!(e instanceof ReviewConcurrencyError)) {
                    throw e;
                }
            }
            console.log('Review publish failed due to concurrency issue. Retrying attempt ', i);
            yield delay(5000);
        }

        yield put(loadReviewInfo({ reviewId: reviewSnapshot.reviewId, fileToPreload: action.payload.fileToLoad }));
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
    loadFileDiffSaga,
    loadReviewInfoSaga,
    createGitLabLinkSaga,
    publishReviewSaga,
    mergePullRequestSaga,
];
