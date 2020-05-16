import { take, put, select, delay, race } from "redux-saga/effects";
import {
    selectFileForView,
    loadedFileDiff,
    loadReviewInfo,
    loadedReviewInfo,
    publishReview,
    mergePullRequest,
    MergePullRequestArgs,
    PublishReviewArgs,
    clearUnpublishedReviewInfo,
    reviewFile,
    markEmptyFilesAsReviewed,
    saveVSCodeWorkspace,
    loadedVsCodeWorkspace,
    changeFileRange,
    resolveRevision2,
} from './state';
import { Action } from "typescript-fsa";
import notify from '../../notify';
import { ReviewerApi, ReviewInfo, ReviewId, ReviewSnapshot, ReviewConcurrencyError, MergeFailedError, FileId, FileToReview, FileDiff, DiffDiscussions } from '../../api/reviewer';
import { RootState } from "../../rootState";
import { startOperation, stopOperation, setOperationMessage } from "../../loading/saga";
import { getUnpublishedReview, getReviewVSCodeWorkspace, saveReviewVSCodeWorkspace, LocallyStoredReview } from "./storage";
import { RevisionId } from "@api/revisionId";

function* markNotChangedAsViewed() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ fileId: FileId }> = yield take(markEmptyFilesAsReviewed);

        yield startOperation();

        const state = yield select((s: RootState) => ({
            currentReview: s.review.currentReview,
            reviewedFiles: s.review.reviewedFiles
        }));

        const currentReview: ReviewInfo = state.currentReview;
        const reviewedFiles: FileId[] = state.reviewedFiles;

        console.log(`Marking empty files as reviewed. ${currentReview.filesToReview.length} files to check`);

        let fileIndex = 0;
        let markAsReviewedCount = 0;
        const progressRefreshStep = Math.ceil(currentReview.filesToReview.length * 5 / 100);

        for (const file of currentReview.filesToReview) {
            if (fileIndex % progressRefreshStep == 0) {
                yield setOperationMessage(`Scanning for empty files: ${fileIndex + 1}/${currentReview.filesToReview.length}`);
                yield delay(1);
            }

            fileIndex++;

            const fileId = file.fileId;

            if (reviewedFiles.indexOf(fileId) >= 0) {
                console.log({ message: "Already marked as reviewed", file: file });
                continue; // already mark as reviewed
            }

            const currentRange = {
                reviewId: state.currentReview.reviewId,
                range: {
                    previous: resolveRevision2(state.currentReview, file.previous),
                    current: resolveRevision2(state.currentReview, file.current)
                },
                path: file.diffFile,
                fileId: file.fileId
            }

            const diff: FileDiff = yield api.getDiff(currentRange.reviewId, currentRange.range, currentRange.path);
            const remappedDiscussions: DiffDiscussions = yield api.getDiffDiscussions(currentRange.reviewId, currentRange.range, currentRange.fileId, currentRange.path.newPath);

            const isEmpty = (diff.hunks == null || diff.hunks.length == 0) && remappedDiscussions.remapped.length == 0;

            if (isEmpty) {
                console.log({ message: "Marking automatically as reviewed", file: file });
                markAsReviewedCount++;
                yield put(reviewFile({ path: file.reviewFile }));
            } else {
                console.log({ message: "File Not empty", file: file });
            }
        }

        console.log("Marking empty files as reviewed... Finished");

        yield stopOperation();

        notify.success(`Marked ${markAsReviewedCount} files as reviewed.`);
    }
}

function* loadFileDiffSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action = yield race({
            selectFileForView: take(selectFileForView),
            changeFielRange: take(changeFileRange)
        });

        const currentRange = yield select((state: RootState) => ({
            reviewId: state.review.currentReview.reviewId,
            range: {
                previous: resolveRevision2(state.review.currentReview, state.review.selectedFile.range.previous),
                current: resolveRevision2(state.review.currentReview, state.review.selectedFile.range.current),
            },
            path: state.review.selectedFile.fileToReview.diffFile,
            fileId: state.review.selectedFile.fileId
        }));


        yield startOperation(`Loading file ${currentRange.path.newPath} ...`);


        const diff = yield api.getDiff(currentRange.reviewId, currentRange.range, currentRange.path);
        const remappedDiscussions = yield api.getDiffDiscussions(currentRange.reviewId, currentRange.range, currentRange.fileId, currentRange.path.newPath);

        yield put(loadedFileDiff({
            diff: diff,
            remappedDiscussions: remappedDiscussions
        }));

        yield stopOperation();
    }
}

function* loadReviewInfoSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ reviewId: ReviewId, fileToPreload?: FileId }> = yield take(loadReviewInfo);
        yield startOperation();

        const info: ReviewInfo = yield api.getReviewInfo(action.payload.reviewId);

        const unpublishedInfo: LocallyStoredReview = getUnpublishedReview(action.payload.reviewId);


        const vsCodeWorkspace = getReviewVSCodeWorkspace(action.payload.reviewId)

        const currentReview: ReviewId = yield select((s: RootState) => s.review.currentReview ? s.review.currentReview.reviewId : null);

        yield put(loadedReviewInfo({ info, unpublishedInfo: unpublishedInfo.unpublished, fileIdMap: unpublishedInfo.fileIdMap, vsCodeWorkspace }));

        if (action.payload.fileToPreload) {
            const file = info.filesToReview.find(f => f.fileId == action.payload.fileToPreload)
            if (file != null) {
                yield put(selectFileForView({ fileId: file.fileId }));
            }
        }

        yield stopOperation();
    }
}

function* publishReviewSaga() {
    const api = new ReviewerApi();
    for (; ;) {
        const action: Action<PublishReviewArgs> = yield take(publishReview);

        yield startOperation("Publishing...");

        const reviewSnapshot: ReviewSnapshot = yield select((s: RootState): ReviewSnapshot => ({
            reviewId: s.review.currentReview.reviewId,
            revision: {
                base: s.review.currentReview.baseCommit,
                head: s.review.currentReview.headCommit
            },
            startedFileDiscussions: s.review.unpublishedFileDiscussions.map(d => ({
                targetRevisionId: RevisionId.mapLocalToRemote(d.revision, s.review.currentReview.headCommit),
                temporaryId: d.comment.id,
                fileId: d.fileId,
                lineNumber: d.lineNumber,
                state: d.state,
                content: d.comment.content
            })),
            startedReviewDiscussions: s.review.unpublishedReviewDiscussions.map(d => ({
                targetRevisionId: RevisionId.mapLocalToRemote(d.revision, s.review.currentReview.headCommit),
                temporaryId: d.comment.id,
                content: d.comment.content,
                state: d.state,
            })),
            resolvedDiscussions: s.review.unpublishedResolvedDiscussions,
            replies: s.review.unpublishedReplies,
            reviewedFiles: s.review.unpublishedReviewedFiles.map(f => ({
                ...f,
                revision: RevisionId.mapLocalToRemote(f.revision, s.review.currentReview.headCommit)
            })),
            unreviewedFiles: s.review.unpublishedUnreviewedFiles.map(f => ({
                ...f,
                revision: RevisionId.mapLocalToRemote(f.revision, s.review.currentReview.headCommit)
            })),
        }));

        let successfulPublish = false;
        for (let i = 0; i < 100; i++) {
            try {
                yield api.publishReview(reviewSnapshot);
                successfulPublish = true;
                break;
            } catch (e) {
                if (!(e instanceof ReviewConcurrencyError)) {
                    throw e;
                }
            }
            console.log('Review publish failed due to concurrency issue. Retrying attempt ', i);
            yield delay(5000);
        }

        if (successfulPublish) {
            yield put(clearUnpublishedReviewInfo({ reviewId: reviewSnapshot.reviewId }));
        }

        yield put(loadReviewInfo({ reviewId: reviewSnapshot.reviewId, fileToPreload: action.payload.fileToLoad }));

        yield stopOperation();

        notify.success('Your review has been published');
    }
}

function* mergePullRequestSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<MergePullRequestArgs> = yield take(mergePullRequest);

        yield startOperation("Merging...");

        try {
            yield api.mergePullRequest(action.payload.reviewId, action.payload.shouldRemoveBranch, action.payload.commitMessage);
        } catch (e) {
            if (!(e instanceof MergeFailedError)) {
                throw e;
            }

            notify.error('Merge failed. Check merge request page to see what\'s wrong');
        }

        yield put(loadReviewInfo({ reviewId: action.payload.reviewId }));

        yield stopOperation();
    }
}

function* saveVSCodeWorkspaceSaga() {
    for (; ;) {
        const action: Action<{ vsCodeWorkspace: string }> = yield take(saveVSCodeWorkspace);
        const currentReviewId = yield select((s: RootState): ReviewId => s.review.currentReview.reviewId);

        saveReviewVSCodeWorkspace(currentReviewId, action.payload.vsCodeWorkspace);

        yield put(loadedVsCodeWorkspace({ vsCodeWorkspace: action.payload.vsCodeWorkspace }));
    }
}

export default [
    loadFileDiffSaga,
    loadReviewInfoSaga,
    publishReviewSaga,
    mergePullRequestSaga,
    markNotChangedAsViewed,
    saveVSCodeWorkspaceSaga
];
