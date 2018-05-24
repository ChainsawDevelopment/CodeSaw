import * as React from "react";

import { Dispatch } from "redux";
import { RevisionRange, selectCurrentRevisions, selectFileForView } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import { ChangedFile, RevisionRangeInfo } from "../../api/reviewer";

import Sidebar from 'semantic-ui-react/dist/commonjs/modules/Sidebar';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';

import VersionSelector from './versionSelector';
import ChangedFileTree from './changedFileTree';

import "./review.less";

type SelectFileForViewHandler = (path: string) => void;

const RangeInfo = (props: { info: RevisionRangeInfo, selectedFile: string, onSelectFileForView: SelectFileForViewHandler }): JSX.Element => {
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
                        file diff
                    </Segment>
                </Sidebar.Pusher>
            </Sidebar.Pushable>
        </div>
    );
}

interface OwnProps {
    reviewId: number;
}

interface DispatchProps {
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
}

interface StateProps {
    availableRevisions: number[];
    currentRange: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: string;
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props): JSX.Element => {
    return (
        <div id="review-page">
            <h1>Review {props.reviewId}</h1>

            <VersionSelector
                available={props.availableRevisions}
                range={props.currentRange}
                onSelectRange={props.selectRevisionRange}
            />
            {props.rangeInfo ? (<RangeInfo 
                info={props.rangeInfo} 
                selectedFile={props.selectedFile}
                onSelectFileForView={props.selectFileForView} 
            />) : null}
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    availableRevisions: state.review.availableRevisions,
    currentRange: state.review.range,
    rangeInfo: state.review.rangeInfo,
    selectedFile: state.review.selectedFile
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: path => dispatch(selectFileForView({ path }))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);