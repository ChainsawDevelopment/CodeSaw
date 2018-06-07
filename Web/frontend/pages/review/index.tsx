import * as React from "react";

import { Dispatch } from "redux";
import { selectCurrentRevisions, selectFileForView, loadReviewInfo, rememberRevision, FileInfo } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import { ChangedFile, RevisionRangeInfo, FileDiff, ReviewInfo, RevisionRange, ReviewId, RevisionId, Review, Hunk, PathPair, emptyPathPair } from "../../api/reviewer";

import Sidebar from 'semantic-ui-react/dist/commonjs/modules/Sidebar';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Message from 'semantic-ui-react/dist/commonjs/collections/Message';

import VersionSelector from './versionSelector';
import ChangedFileTree from './changedFileTree';
import DiffView from './diffView';

import "./review.less";
import { OnMount } from "../../components/OnMount";

type SelectFileForViewHandler = (path: PathPair) => void;

const FileSummary = (props: {file: FileInfo}): JSX.Element => {
    const items: JSX.Element[] = [];

    if (props.file.treeEntry.renamedFile) {
        const { path } = props.file.treeEntry;

        items.push(
            <div key="renamed" className="renamed">File renamed <pre>{path.oldPath}</pre> &rarr; <pre>{path.newPath}</pre></div>
        );
    }

    if(items.length == 0) {
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
            <Sidebar.Pushable as={Segment}>
                <Sidebar visible={true} width='thin'>
                    <ChangedFileTree
                        paths={props.info.changes.map(i => i.path)}
                        selected={props.selectedFile ? props.selectedFile.path : emptyPathPair}
                        onSelect={props.onSelectFileForView}
                    />
                </Sidebar>
                <Sidebar.Pusher>
                    <Segment basic>
                        <Button onClick={() => props.onSelectFileForView(props.selectedFile.path)}>Refresh diff</Button>
                        {props.selectedFile ? <FileSummary file={props.selectedFile} /> : null}
                        {props.selectedFile && props.selectedFile.diff ? <DiffView hunks={props.selectedFile.diff.hunks} /> : null}
                    </Segment>
                </Sidebar.Pusher>
            </Sidebar.Pushable>
        </div>
    );
}

interface OwnProps {
    reviewId: ReviewId;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
    rememberRevision(reviewId: ReviewId, head: string, base: string);
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

    const rememberVersion = !props.currentReview.hasProvisionalRevision ? null : (
        <div>
            <Button onClick={() => props.rememberRevision(props.reviewId, props.currentReview.headCommit, props.currentReview.baseCommit)}>Remember revision</Button>
        </div>
    );

    return (
        <div id="review-page">
            <OnMount onMount={() => props.loadReviewInfo(props.reviewId)} />

            <h1>Review {props.currentReview.title}</h1>

            <VersionSelector
                available={['base', ...props.currentReview.pastRevisions, ...provisional]}
                hasProvisonal={props.currentReview.hasProvisionalRevision}
                range={props.currentRange}
                onSelectRange={props.selectRevisionRange}
            />
            {rememberVersion}
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
    rememberRevision: (reviewId, head, base) => dispatch(rememberRevision({ reviewId, head, base }))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);