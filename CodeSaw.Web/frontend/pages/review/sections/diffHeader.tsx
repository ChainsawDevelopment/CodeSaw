import { connect } from "react-redux";
import * as React from 'react';
import { Menu } from "semantic-ui-react";
import { PublishButton } from "../PublishButton";
import ChangedFileTreePopup from "../fileTreePopup";
import { RootState } from "@src/rootState";
import { FileToReview, FileId, FileDiscussion, ReviewId, ReviewInfo, RevisionId } from "@api/reviewer";
import { FileInfo, reviewFile, unreviewFile, changeFileRange } from "../state";
import { SelectFileForViewHandler } from "../selectFile";
import FileList from '@src/fileList';
import * as RIMenu from './rangeInfo_menu';
import * as PathPairs from "@src/pathPair";
import { Dispatch } from "redux";
import RangeSelector from "@src/components/RangeSelector";

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
    revisions: {
        number: number;
        head: string;
        base: string;
    }[];
    hasProvisional: boolean;
    base: string;
    head: string;
}

interface DispatchProps {
    review(file: PathPairs.PathPair): void;
    unreview(file: PathPairs.PathPair): void;
    changeFileRange(previous: RevisionId, current: RevisionId): void;
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

        const revisions = [
            { label: '&perp;', id: 'base' as RevisionId },
            ...props.revisions.map(r => ({
                label: r.number.toString(),
                id: r.number as RevisionId
            }))
        ];

        if (props.hasProvisional) {
            revisions.push({
                label: 'P',
                id: 'provisional' as RevisionId
            });
        }

        const startIndex = revisions.findIndex(r => r.id == props.selectedFile.range.previous);
        let endIndex = revisions.findIndex(r => r.id == props.selectedFile.range.current);
        if (endIndex == -1 && props.selectedFile.range.current == props.head) {
            endIndex = revisions.findIndex(r => r.id == 'provisional');
        }

        const selectors = revisions.map(r => <span key={r.id} dangerouslySetInnerHTML={{ __html: r.label }} />)

        const onChange = (s: number, e: number) => {
            props.changeFileRange(revisions[s].id, revisions[e].id);
        }

        menuItems.push(<Menu.Item fitted key="revision-select">
            <RangeSelector start={startIndex} end={endIndex} onChange={onChange}>
                {selectors}
            </RangeSelector>
        </Menu.Item>);
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
        reviewId: state.review.currentReview.reviewId,
        revisions: state.review.currentReview.pastRevisions,
        hasProvisional: state.review.currentReview.hasProvisionalRevision,
        head: state.review.currentReview.headCommit,
        base: state.review.currentReview.baseCommit
    }),
    (dispatch: Dispatch): DispatchProps => ({
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
        changeFileRange: (previous, current) => dispatch(changeFileRange({ previous, current }))
    })
)(DiffHeader);