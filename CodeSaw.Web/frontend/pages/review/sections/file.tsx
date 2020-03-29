import { connect } from "react-redux";
import * as React from "react";
import RangeInfo, { ReviewFileActions, SelectFileForViewHandler, OnShowFileHandlerAvailable } from "../rangeInfo";
import { Dispatch } from "redux";
import { RootState } from "@src/rootState";
import { ReviewInfo, FileId, ReviewId } from "@api/reviewer";
import { markEmptyFilesAsReviewed, reviewFile, unreviewFile, publishReview, FileInfo, selectFileForView } from "../state";
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
    selectedFile: FileInfo;
}

interface DispatchProps {
    markNonEmptyAsViewed(): void;
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
        markNonEmptyAsViewed={props.markNonEmptyAsViewed}
    />
};

export default connect(
    (state: RootState): StateProps => ({
        currentReview: state.review.currentReview,
        reviewedFiles: state.review.reviewedFiles,
        selectedFile: state.review.selectedFile,
    }),
    (dispatch: Dispatch, ownProps: OwnProps) => ({
        selectFileForView: (fileId) => dispatch(selectFileForView({ fileId })),
        markNonEmptyAsViewed: () => dispatch(markEmptyFilesAsReviewed({})),
        reviewFile: {
            review: (path) => dispatch(reviewFile({ path })),
            unreview: (path) => dispatch(unreviewFile({ path })),
        },
        publishReview: () => dispatch(publishReview({ fileToLoad: ownProps.fileId })),
    })
)(File);
