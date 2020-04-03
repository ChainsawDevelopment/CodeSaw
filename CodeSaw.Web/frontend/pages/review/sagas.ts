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
    ReviewState,
    FileReviewStatusChange,
    reviewFile,
    markEmptyFilesAsReviewed,
    resolveRevision,
    saveVSCodeWorkspace,
    loadedVsCodeWorkspace,
    changeFileRange
} from './state';
import { Action } from "typescript-fsa";
import notify from '../../notify';
import { ReviewerApi, ReviewInfo, ReviewId, RevisionId, RevisionRange, ReviewSnapshot, ReviewConcurrencyError, MergeFailedError, FileId, FileToReview, FileDiff, DiffDiscussions } from '../../api/reviewer';
import { RootState } from "../../rootState";
import { startOperation, stopOperation, setOperationMessage } from "../../loading/saga";
import { getUnpublishedReview, getReviewVSCodeWorkspace, saveReviewVSCodeWorkspace } from "./storage";

function* markNotChangedAsViewed() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ fileId: FileId }> = yield take(markEmptyFilesAsReviewed);

        yield startOperation();

        const state = yield select((s: RootState) => ({
            currentReview: s.review.currentReview,
            unpublishedReviewedFiles: s.review.unpublishedReviewedFiles,
            unpublishedUnreviewedFiles: s.review.unpublishedUnreviewedFiles,
            reviewedFiles: s.review.reviewedFiles
        }));

        const currentReview: ReviewInfo = state.currentReview;
        const reviewedFiles: FileId[] = state.reviewedFiles;
        const unpublishedReviewedFiles: FileReviewStatusChange = state.unpublishedReviewedFiles;

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
            const fileUnpublishedReviewedFiles = state.unpublishedReviewedFiles[fileId] || [];

            if (reviewedFiles.indexOf(fileId) >= 0 || fileUnpublishedReviewedFiles.indexOf(fileId) >= 0) {
                console.log({ message: "Already marked as reviewed", file: file });
                continue; // already mark as reviewed
            }

            const currentRange = {
                reviewId: state.currentReview.reviewId,
                range: {
                    previous: resolveRevision(state.currentReview, file.previous),
                    current: resolveRevision(state.currentReview, file.current)
                },
                path: file.diffFile,
                fileId: file.fileId
            }

            const diff: FileDiff = yield api.getDiff(currentRange.reviewId, currentRange.range, currentRange.path);
            const remappedDiscussions: DiffDiscussions = yield api.getDiffDiscussions(currentRange.reviewId, currentRange.range, currentRange.fileId, currentRange.path.newPath);

            const isEmpty = diff.hunks.length == 0 && remappedDiscussions.remapped.length == 0;

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
            range: state.review.selectedFile.range,
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

        const unpublishedInfo = getUnpublishedReview(action.payload.reviewId);


        const vsCodeWorkspace = getReviewVSCodeWorkspace(action.payload.reviewId)

        const currentReview: ReviewId = yield select((s: RootState) => s.review.currentReview ? s.review.currentReview.reviewId : null);

        yield put(loadedReviewInfo({ info, unpublishedInfo, vsCodeWorkspace }));

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
                targetRevisionId: d.revision,
                temporaryId: d.comment.id,
                fileId: d.fileId,
                lineNumber: d.lineNumber,
                state: d.state,
                content: d.comment.content
            })),
            startedReviewDiscussions: s.review.unpublishedReviewDiscussions.map(d => ({
                targetRevisionId: d.revision,
                temporaryId: d.comment.id,
                content: d.comment.content,
                state: d.state,
            })),
            resolvedDiscussions: s.review.unpublishedResolvedDiscussions,
            replies: s.review.unpublishedReplies,
            reviewedFiles: s.review.unpublishedReviewedFiles,
            unreviewedFiles: s.review.unpublishedUnreviewedFiles
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
