import { connect } from "react-redux";
import * as React from 'react';
import { Menu } from "semantic-ui-react";
import { PublishButton } from "../PublishButton";
import ChangedFileTreePopup from "../fileTreePopup";
import { RootState } from "@src/rootState";
import { FileToReview, FileId, FileDiscussion, ReviewId } from "@api/reviewer";
import { FileInfo, reviewFile, unreviewFile, changeFileRange } from "../state";
import { SelectFileForViewHandler } from "../selectFile";
import FileList from '@src/fileList';
import * as RIMenu from './rangeInfo_menu';
import * as PathPairs from "@src/pathPair";
import { Dispatch } from "redux";
import RangeSelector from "@src/components/RangeSelector";
import { LocalRevisionId, RevisionId } from "@api/revisionId";
import Popup from "@ui/modules/Popup";
import Icon from '@ui/elements/Icon';

interface OwnProps {
    onSelectFileForView: SelectFileForViewHandler;

    sidebarVisible: boolean;
    setSidebarVisible(visible: boolean): void;
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
    changeFileRange(previous: LocalRevisionId, current: LocalRevisionId): void;
}

type Props = StateProps & DispatchProps & OwnProps;

const getSelector = (revision: LocalRevisionId) => {
    if(RevisionId.isBase(revision)) {
        return <span key='base' dangerouslySetInnerHTML={{ __html: '&perp;' }} />;
    }

    if(RevisionId.isProvisional(revision)) {
        return <span key='provisional'>P</span>;
    }

    return <span key={revision.revision}>{revision.revision}</span>;
}

const ToggleSidebar = (props: {visible: boolean; set(visible: boolean): void}) => {
    if(props.visible) {
        return <Menu.Item key="toggle-sidebar" fitted>
            <Popup
                trigger={<Icon onClick={() => props.set(false)} name="angle double left" circular link color="blue"></Icon>}
                content="Hide sidebar"
            />
        </Menu.Item>;
    } else {
        return <Menu.Item key="toggle-sidebar" fitted>
            <Popup
                trigger={<Icon onClick={() => props.set(true)} name="angle double right" circular link color="blue"></Icon>}
                content="Show sidebar"
            />
        </Menu.Item>;
    }
}

const DiffHeader = (props: Props): JSX.Element => {
    const menuItems = [];

    const { selectedFile } = props;

    menuItems.push(<ToggleSidebar key="toggle-sidebar" visible={props.sidebarVisible} set={props.setSidebarVisible} />);

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

        const revisions: LocalRevisionId[] = [
            RevisionId.Base,
            ...props.revisions.map(r => RevisionId.makeSelected(r.number))
        ];

        if (props.hasProvisional) {
            revisions.push(RevisionId.Provisional);
        }

        const startIndex2 = revisions.findIndex(r => RevisionId.equal(r, props.selectedFile.range.previous));
        const endIndex2 = revisions.findIndex(r => RevisionId.equal(r, props.selectedFile.range.current));

        const selectors2 = revisions.map(getSelector);

        const onChange = (s: number, e: number) => {
            props.changeFileRange(revisions[s], revisions[e]);
        }

        menuItems.push(<Menu.Item fitted key="revision-select">
            <RangeSelector start={startIndex2} end={endIndex2} onChange={onChange}>
                {selectors2}
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