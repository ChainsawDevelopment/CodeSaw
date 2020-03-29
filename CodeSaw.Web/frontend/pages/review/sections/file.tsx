import { connect } from "react-redux";
import * as React from "react";
import RangeInfo, { ReviewFileActions, SelectFileForViewHandler, OnShowFileHandlerAvailable } from "../rangeInfo";
import { Dispatch } from "redux";
import { RootState, UserState } from "@src/rootState";
import { ReviewInfo, FileId, CommentReply, FileDiscussion, ReviewId } from "@api/reviewer";
import { markEmptyFilesAsReviewed, reviewFile, unreviewFile, publishReview, replyToComment, editUnpublishedComment, removeUnpublishedComment, resolveDiscussion, unresolveDiscussion, startReviewDiscussion, startFileDiscussion, FileInfo, selectFileForView } from "../state";
import { DiscussionActions } from "../commentsView";
import { createLinkToFile } from "../FileLink";
import { History } from "history";

interface OwnProps {
    reviewId: ReviewId;
    fileId?: FileId;
    history: History;
    showFileHandler?(): void;
    onShowFileHandlerAvailable: OnShowFileHandlerAvailable;
}

interface StateProps {
    currentReview: ReviewInfo;
    reviewedFiles: FileId[];
    currentUser: UserState;
    unpublishedReplies: CommentReply[];
    unpublishedFileDiscussion: FileDiscussion[];
    unpublishedResolvedDiscussions: string[];
    selectedFile: FileInfo;
}

interface DispatchProps {
    markNonEmptyAsViewed(): void;
    discussionActions: DiscussionActions;
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean, currentUser?: UserState): void;
    publishReview(): void;
    reviewFile: ReviewFileActions;
    selectFileForView: SelectFileForViewHandler;
}

type Props = StateProps & DispatchProps & OwnProps;

const File = (props: Props): JSX.Element => {
    const selectNewFileForView = (fileId: FileId) => {
        if (fileId != null) {
            props.selectFileForView(fileId);

            const fileLink = createLinkToFile(props.reviewId, fileId);
            if (fileLink != window.location.pathname) {
                props.history.push(fileLink);
            }

            if (props.showFileHandler) {
                props.showFileHandler();
            }
        }
    };

    const selectedFile = props.selectedFile ?
        { ...props.selectedFile, isReviewed: props.reviewedFiles.indexOf(props.selectedFile.fileId) >= 0 }
        : null;

    return <RangeInfo
        filesToReview={props.currentReview.filesToReview}
        selectedFile={selectedFile}
        onSelectFileForView={selectNewFileForView}
        reviewFile={props.reviewFile}
        reviewedFiles={props.reviewedFiles}
        publishReview={props.publishReview}
        onShowFileHandlerAvailable={props.onShowFileHandlerAvailable}
        fileComments={props.currentReview.fileDiscussions}
        startFileDiscussion={props.startFileDiscussion}
        unpublishedFileDiscussion={props.unpublishedFileDiscussion}
        commentActions={props.discussionActions}
        pendingResolved={props.unpublishedResolvedDiscussions}
        unpublishedReplies={props.unpublishedReplies}
        currentUser={props.currentUser}
        markNonEmptyAsViewed={props.markNonEmptyAsViewed}
    />
};

export default connect(
    (state: RootState): StateProps => ({
        currentUser: state.currentUser,
        currentReview: state.review.currentReview,
        reviewedFiles: state.review.reviewedFiles,
        unpublishedFileDiscussion: state.review.unpublishedFileDiscussions,
        unpublishedResolvedDiscussions: state.review.unpublishedResolvedDiscussions,
        unpublishedReplies: state.review.unpublishedReplies,
        selectedFile: state.review.selectedFile,
    }),
    (dispatch: Dispatch, ownProps: OwnProps) => ({
        selectFileForView: (fileId) => dispatch(selectFileForView({ fileId })),
        markNonEmptyAsViewed: () => dispatch(markEmptyFilesAsReviewed({})),
        startFileDiscussion: (fileId, lineNumber, content, needsResolution, currentUser) => dispatch(startFileDiscussion({ fileId, lineNumber, content, needsResolution, currentUser })),
        reviewFile: {
            review: (path) => dispatch(reviewFile({ path })),
            unreview: (path) => dispatch(unreviewFile({ path })),
        },
        publishReview: () => dispatch(publishReview({ fileToLoad: ownProps.fileId })),
        addNew: (content, needsResolution, currentUser) => dispatch(startReviewDiscussion({ content, needsResolution, currentUser })),
        discussionActions: {
            addReply: (parentId, content) => dispatch(replyToComment({ parentId, content })),
            editReply: (commentId, content) => dispatch(editUnpublishedComment({ commentId, content })),
            removeUnpublishedComment: (commentId) => dispatch(removeUnpublishedComment({ commentId })),
            resolve: (discussionId) => dispatch(resolveDiscussion({ discussionId })),
            unresolve: (discussionId) => dispatch(unresolveDiscussion({ discussionId })),
        }
    }),
    (stateProps: StateProps, dispatchProps, ownProps: OwnProps): Props => ({
        ...stateProps,
        ...dispatchProps,
        ...ownProps,
        discussionActions: {
            ...dispatchProps.discussionActions,
            addNew: (content, needsResolution) => dispatchProps.addNew(content, needsResolution, stateProps.currentUser),
        },
        startFileDiscussion: (path, lineNumber, content, needsResolution) => dispatchProps.startFileDiscussion(path, lineNumber, content, needsResolution, stateProps.currentUser),
    })
)(File);
