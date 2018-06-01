import * as React from "react";

import { Dispatch } from "redux";
import { selectCurrentRevisions, selectFileForView, loadReviewInfo, rememberRevision } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import { ChangedFile, RevisionRangeInfo, FileDiff, DiffChunk, ReviewInfo, RevisionRange, ReviewId, RevisionId, Review } from "../../api/reviewer";

import Sidebar from 'semantic-ui-react/dist/commonjs/modules/Sidebar';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';

import VersionSelector from './versionSelector';
import ChangedFileTree from './changedFileTree';
import DiffView from './diffView';

import "./review.less";
import { OnMount } from "../../components/OnMount";

type SelectFileForViewHandler = (path: string) => void;

const RangeInfo = (props: { info: RevisionRangeInfo, selectedFile: string, chunks: DiffChunk[], onSelectFileForView: SelectFileForViewHandler }): JSX.Element => {
    return (
        <div style={{ flex: 1 }}>
            <Sidebar.Pushable as={Segment}>
                <Sidebar visible={true} width='thin'>
                    <ChangedFileTree
                        paths={props.info.changes.map(i => i.newPath)}
                        selected={props.selectedFile}
                        onSelect={props.onSelectFileForView}
                    />
                </Sidebar>
                <Sidebar.Pusher>
                    <Segment basic>
                        {props.chunks ? <DiffView chunks={props.chunks} /> : null}
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
    selectedFile: string;
    selectedFileDiff: FileDiff;
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
                chunks={props.selectedFileDiff ? props.selectedFileDiff.chunks : null}
            />) : null}
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentReview: state.review.currentReview,
    currentRange: state.review.range,
    rangeInfo: state.review.rangeInfo,
    selectedFile: state.review.selectedFile,
    selectedFileDiff: state.review.selectedFileDiff
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId) => dispatch(loadReviewInfo({ reviewId })),
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: path => dispatch(selectFileForView({ path })),
    rememberRevision: (reviewId, head, base) => dispatch(rememberRevision({ reviewId, head, base }))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);