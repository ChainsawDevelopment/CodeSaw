import { connect } from "react-redux";
import * as React from 'react';
import { Menu } from "semantic-ui-react";
import { PublishButton } from "../PublishButton";
import ChangedFileTreePopup from "../fileTreePopup";
import { RootState } from "@src/rootState";
import { FileToReview, FileId, FileDiscussion, ReviewId, ReviewInfo } from "@api/reviewer";
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
    changeFileRange(previous: { head: string; base: string }, current: { head: string; base: string }): void;
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
            { number: '&perp;', head: props.base, base: props.base },
            ...props.revisions.map(r => ({
                ...r,
                number: r.number.toString(),
            }))
        ];

        if (props.hasProvisional) {
            revisions.push({
                number: 'P',
                head: props.head,
                base: props.base
            });
        }

        const startIndex = revisions.findIndex(r => r.base == props.selectedFile.range.previous.base && r.head == props.selectedFile.range.previous.head);
        const endIndex = revisions.findIndex(r => r.base == props.selectedFile.range.current.base && r.head == props.selectedFile.range.current.head);

        const selectors = revisions.map(r => <span key={r.number} dangerouslySetInnerHTML={{ __html: r.number }} />)

        const onChange = (s: number, e: number) => {
            props.changeFileRange({
                base: revisions[s].base,
                head: revisions[s].head
            }, {
                base: revisions[e].base,
                head: revisions[e].head
            });
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