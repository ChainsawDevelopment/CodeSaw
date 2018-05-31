import * as React from "react";

import { Dispatch } from "redux";
import { RevisionRange, selectCurrentRevisions, selectFileForView, loadReviewInfo, RevisionId } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import { ChangedFile, RevisionRangeInfo, FileDiff, DiffChunk, ReviewInfo } from "../../api/reviewer";

import Sidebar from 'semantic-ui-react/dist/commonjs/modules/Sidebar';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';

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
                        paths={props.info.changes.map(i => i.path)}
                        selected={props.selectedFile}
                        onSelect={props.onSelectFileForView}
                    />
                </Sidebar>
                <Sidebar.Pusher>
                    <Segment basic>
                        {props.chunks ? <DiffView chunks={props.chunks}/> : null}
                    </Segment>
                </Sidebar.Pusher>
            </Sidebar.Pushable>
        </div>
    );
}

interface OwnProps {
    projectId: number;
    reviewId: number;
}

interface DispatchProps {
    loadReviewInfo(projectId: number, reviewId: number): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
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
    return (
        <div id="review-page">
            <OnMount onMount={() => props.loadReviewInfo(props.projectId, props.reviewId)}/>

            <h1>Review {props.currentReview.title}</h1>

            <VersionSelector
                available={['base', ...props.currentReview.pastRevisions, 'provisional']}
                hasProvisonal={props.currentReview.hasProvisionalRevision}
                range={props.currentRange}
                onSelectRange={props.selectRevisionRange}
            />
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
    loadReviewInfo: (projectId: number, reviewId: number) => dispatch(loadReviewInfo({projectId, reviewId})),
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: path => dispatch(selectFileForView({ path }))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);