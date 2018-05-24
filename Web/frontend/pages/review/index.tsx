import * as React from "react";

import VersionSelector from './versionSelector';
import { Dispatch } from "redux";
import { selectCurrentVersion, selectPreviousVersion, RevisionRange } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";

interface OwnProps {
    reviewId: number;
}

interface DispatchProps {
    selectPreviousVersion(revision: number): void;
    selectCurrentVersion(revision: number): void;
}

interface StateProps {
    availableRevisions: number[];
    currentRange: RevisionRange;
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props): JSX.Element => {
    return (
        <div>
            <h1>Review {props.reviewId}</h1>

            <VersionSelector
                available={props.availableRevisions}
                range={props.currentRange}
                onSelectPrevious={props.selectPreviousVersion}
                onSelectCurrent={props.selectCurrentVersion}
            />
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    availableRevisions: state.review.availableRevisions,
    currentRange: state.review.range
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    selectPreviousVersion: r => dispatch(selectPreviousVersion({ revision: r })),
    selectCurrentVersion: r => dispatch(selectCurrentVersion({ revision: r })),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);