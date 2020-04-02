import { connect } from "react-redux";
import * as React from 'react';
import { Menu } from "semantic-ui-react";
import { PublishButton } from "../PublishButton";
import ChangedFileTreePopup from "../fileTreePopup";
import { RootState } from "@src/rootState";
import { FileToReview, FileId, FileDiscussion, ReviewId } from "@api/reviewer";
import { FileInfo, reviewFile, unreviewFile } from "../state";
import { SelectFileForViewHandler } from "../selectFile";
import FileList from '@src/fileList';
import * as RIMenu from './rangeInfo_menu';
import * as PathPairs from "@src/pathPair";
import { Dispatch } from "redux";

interface OwnProps {
    onSelectFileForView: SelectFileForViewHandler;
}

interface StateProps {
    filesToReview: FileToReview[];
    selectedFile: FileInfo & { isReviewed: boolean };
    reviewedFiles: FileId[];
    fileComments: FileDiscussion[];
    reviewId: ReviewId;
    vsCodeWorkspace: string;
}

interface DispatchProps {
    review(file: PathPairs.PathPair): void;
    unreview(file: PathPairs.PathPair): void;
}

type Props = StateProps & DispatchProps & OwnProps;

const DiffHeader = (props: Props): JSX.Element => {
    const menuItems = [];

    const { selectedFile } = props;

    if (selectedFile) {
        const fileList = new FileList(
            props.filesToReview,
            props.selectedFile.fileId,
            props.reviewedFiles,
            props.fileComments
        );
        const nextFile = fileList.nextUnreviewedFile(+1);
        const prevFile = fileList.nextUnreviewedFile(-1);

        const changeFileReviewState = (newState: boolean) => {
            if (newState) {
                props.review(props.selectedFile.path);
            } else {
                props.unreview(props.selectedFile.path);
            }
        }

        menuItems.push(<RIMenu.ToggleReviewed key="review-mark" isReviewed={props.selectedFile.isReviewed} onChange={changeFileReviewState} />)

        menuItems.push(<RIMenu.RefreshDiff key="refresh-diff" onRefresh={() => props.onSelectFileForView(selectedFile.fileId)} />);

        menuItems.push(<RIMenu.FileNavigation key="file-navigation" reviewId={props.reviewId} prevFile={prevFile} nextFile={nextFile} />);

        menuItems.push(<RIMenu.FilePath key="file-path" path={selectedFile.path} />);

        menuItems.push(<RIMenu.DownloadDiff key="download-diff" diff={props.selectedFile.diff} />);

        if ((props.vsCodeWorkspace || '').length > 0) {
            menuItems.push(<RIMenu.OpenVSCode key="vscode-diff" workspace={props.vsCodeWorkspace} path={props.selectedFile.path} />)
        }
    }

    const selectableFiles = props.filesToReview.map(i => ({ id: i.fileId, name: i.reviewFile }));

    return <Menu secondary id="file-menu">
        {menuItems}
        <Menu.Menu position='right'>
            <Menu.Item>
                <PublishButton />
            &nbsp;
            <ChangedFileTreePopup
                    files={selectableFiles}
                    selected={selectedFile ? selectedFile.fileId : null}
                    reviewedFiles={props.reviewedFiles}
                    onSelect={props.onSelectFileForView}
                    reviewId={props.reviewId}
                />
            </Menu.Item>
        </Menu.Menu>
    </Menu>;
}

export default connect(
    (state: RootState): StateProps => ({
        filesToReview: state.review.currentReview.filesToReview,
        selectedFile: state.review.selectedFile ? {
            ...state.review.selectedFile,
            isReviewed: state.review.reviewedFiles.indexOf(state.review.selectedFile.fileId) >= 0
        } : null,
        reviewedFiles: state.review.reviewedFiles,
        fileComments: state.review.currentReview.fileDiscussions,
        vsCodeWorkspace: state.review.vsCodeWorkspace,
        reviewId: state.review.currentReview.reviewId
    }),
    (dispatch: Dispatch): DispatchProps => ({
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    })
)(DiffHeader);