import * as React from "react";

import { Dispatch } from "redux";
import { selectCurrentRevisions, selectFileForView, loadReviewInfo, FileInfo, publishReview, createGitLabLink } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import { ChangedFile, RevisionRangeInfo, FileDiff, ReviewInfo, RevisionRange, ReviewId, RevisionId, Review, Hunk, PathPair, emptyPathPair } from "../../api/reviewer";

import Sidebar from 'semantic-ui-react/dist/commonjs/modules/Sidebar';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Message from 'semantic-ui-react/dist/commonjs/collections/Message';
import Popup from 'semantic-ui-react/dist/commonjs/modules/Popup';

import VersionSelector from './versionSelector';
import ChangedFileTree from './changedFileTree';
import DiffView from './diffView';
import ChangedFileTreePopup from './fileTreePopup'

import "./review.less";
import { OnMount } from "../../components/OnMount";

type SelectFileForViewHandler = (path: PathPair) => void;

const FileSummary = (props: { file: FileInfo }): JSX.Element => {
    const items: JSX.Element[] = [];

    if (props.file.treeEntry.renamedFile) {
        const { path } = props.file.treeEntry;

        items.push(
            <div key="renamed" className="renamed">File renamed <pre>{path.oldPath}</pre> &rarr; <pre>{path.newPath}</pre></div>
        );
    }

    if (items.length == 0) {
        return null;
    }

    return (
        <Message className="file-summary">
            <Message.Content>
                {items}
            </Message.Content>
        </Message>
    );
};

const RangeInfo = (props: { info: RevisionRangeInfo, selectedFile: FileInfo, onSelectFileForView: SelectFileForViewHandler }): JSX.Element => {   
    return (
        <div style={{ flex: 1 }}>
            <div>
                <ChangedFileTreePopup
                    paths={props.info.changes.map(i => i.path)}
                    selected={props.selectedFile ? props.selectedFile.path : emptyPathPair}
                    onSelect={props.onSelectFileForView}
                />
            </div>
            <Segment basic>
                <Button onClick={() => props.onSelectFileForView(props.selectedFile.path)}>Refresh diff</Button>
                {props.selectedFile ? <FileSummary file={props.selectedFile} /> : null}
                {props.selectedFile && props.selectedFile.diff ? <DiffView hunks={props.selectedFile.diff.hunks} /> : null}
            </Segment>
        </div>
    )
}

interface OwnProps {
    reviewId: ReviewId;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
    createGitLabLink(reviewId: ReviewId);
    publishReview(): void;
}

interface StateProps {
    currentReview: ReviewInfo;
    currentRange: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: FileInfo;
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props): JSX.Element => {
    const provisional: RevisionId[] = props.currentReview.hasProvisionalRevision ? ['provisional'] : [];

    const publishReview = (
        <div>
            <Button onClick={props.publishReview} color='green'>Publish</Button>
        </div>
    );

    const pastRevisions = props.currentReview.pastRevisions.map(i => i.number);

    return (
        <div id="review-page">
            <OnMount onMount={() => props.loadReviewInfo(props.reviewId)} />

            <h1>Review {props.currentReview.title}</h1>

            <VersionSelector
                available={['base', ...pastRevisions, ...provisional]}
                hasProvisonal={props.currentReview.hasProvisionalRevision}
                range={props.currentRange}
                onSelectRange={props.selectRevisionRange}
            />
            <div>
                <Button onClick={() => props.createGitLabLink(props.reviewId)}>Create link in GitLab</Button>
            </div>
            {publishReview}
            {props.rangeInfo ? (<RangeInfo
                info={props.rangeInfo}
                selectedFile={props.selectedFile}
                onSelectFileForView={props.selectFileForView}
            />) : null}
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentReview: state.review.currentReview,
    currentRange: state.review.range,
    rangeInfo: state.review.rangeInfo,
    selectedFile: state.review.selectedFile,
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId) => dispatch(loadReviewInfo({ reviewId })),
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: (path) => dispatch(selectFileForView({ path })),
    createGitLabLink: (reviewId) => dispatch(createGitLabLink({ reviewId })),
    publishReview: () => dispatch(publishReview({}))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);