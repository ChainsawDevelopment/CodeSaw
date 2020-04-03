import { connect } from "react-redux";
import * as React from "react";
import { SelectFileForViewHandler, OnShowFileHandlerAvailable } from "../selectFile";
import { Dispatch } from "redux";
import { RootState } from "@src/rootState";
import { ReviewInfo, FileId, ReviewId } from "@api/reviewer";
import { markEmptyFilesAsReviewed, reviewFile, unreviewFile, publishReview, FileInfo, selectFileForView } from "../state";
import { createLinkToFile } from "../FileLink";
import { History } from "history";
import { HotKeys } from "@src/components/HotKeys";
import Segment from "semantic-ui-react/dist/commonjs/elements/Segment";
import Sticky from "semantic-ui-react/dist/commonjs/modules/Sticky";
import DiffHeader from "./DiffHeader";
import { NoFileView } from "./fileView";
import DiffContent from './diffContent';
import FileList from '@src/fileList';
import scrollToComponent from 'react-scroll-to-component';
import * as PathPairs from "@src/pathPair";

interface ReviewFileActions {
    review(file: PathPairs.PathPair): void;
    unreview(file: PathPairs.PathPair): void;
}

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
    const stickyContainer = React.useRef<HTMLDivElement>();

    const scrollToFile = () => {
        scrollToComponent(stickyContainer.current, { offset: 0, align: 'top', duration: 100, ease: 'linear' })
    };

    const setStickyContainer = React.useCallback((node: HTMLDivElement) => {
        stickyContainer.current = node;
        props.onShowFileHandlerAvailable(scrollToFile);
    }, []);


    const { selectedFile } = props;

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

    const changeFileReviewState = (newState: boolean) => {
        if (newState) {
            props.reviewFile.review(props.selectedFile.path);
        } else {
            props.reviewFile.unreview(props.selectedFile.path);
        }
    }

    let reviewHotKeys = {}

    if (selectedFile) {
        const fileList = new FileList(
            props.currentReview.filesToReview,
            selectedFile.fileId,
            props.reviewedFiles,
            props.currentReview.fileDiscussions
        );
        const nextFile = fileList.nextUnreviewedFile(+1);
        const prevFile = fileList.nextUnreviewedFile(-1);

        const nextFileWithUnresolvedComment = fileList.nextFileWithUnresolvedComment(+1);
        const prevFileWithUnresolvedComment = fileList.nextFileWithUnresolvedComment(-1);

        const isReviewed = props.reviewedFiles.indexOf(props.selectedFile.fileId) >= 0;

        reviewHotKeys = {
            '[': () => prevFile && selectNewFileForView(prevFile.fileId),
            ']': () => nextFile && selectNewFileForView(nextFile.fileId),
            '{': () => prevFileWithUnresolvedComment && selectNewFileForView(prevFileWithUnresolvedComment.fileId),
            '}': () => nextFileWithUnresolvedComment && selectNewFileForView(nextFileWithUnresolvedComment.fileId),
            'y': () => changeFileReviewState(!isReviewed),
            'ctrl+Enter': props.publishReview,
            'ctrl+y': () => props.markNonEmptyAsViewed()
        };
    } else if (props.currentReview.filesToReview.length > 0) {
        const firstFile = props.currentReview.filesToReview[0].fileId;
        const lastFile = props.currentReview.filesToReview[props.currentReview.filesToReview.length - 1].fileId;

        reviewHotKeys = {
            '[': () => lastFile && selectNewFileForView(lastFile),
            ']': () => firstFile && selectNewFileForView(firstFile),
            'ctrl+y': () => props.markNonEmptyAsViewed()
        };
    }

    return <div ref={setStickyContainer}>
        <HotKeys config={reviewHotKeys} />
        <Segment>
            <Sticky context={stickyContainer.current} id="file-sticky">
                <DiffHeader onSelectFileForView={selectNewFileForView} />
            </Sticky>
            <div>
                {selectedFile ?
                    <DiffContent scrollToFile={scrollToFile} />
                    : <NoFileView />
                }
            </div>
        </Segment>
    </div>;
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