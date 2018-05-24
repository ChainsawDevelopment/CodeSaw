import * as React from "react";

import VersionSelector from './versionSelector';
import { Dispatch } from "redux";
import { RevisionRange, selectCurrentRevisions } from "./state";
import { connect } from "react-redux";
import { RootState } from "../../rootState";

interface OwnProps {
    reviewId: number;
}

interface DispatchProps {
    selectRevisionRange(range: RevisionRange): void;
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
                onSelectRange={props.selectRevisionRange}
            />
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    availableRevisions: state.review.availableRevisions,
    currentRange: state.review.range
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range }))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);