import * as React from "react";
import { connect } from 'react-redux';
import { RootState } from "../../rootState";

interface StateProps {
    url: string;
    description: string;
}

const reviewInfoView = (props: StateProps): JSX.Element => {

    // gitlab link
    // build statuses
    // description
    return (
        <div>
            <div>
                <a href={props.url}>Go to merge request page</a>
            </div>
            <div>
                <pre>{props.description}</pre>
            </div>
        </div>
    );
}

const mapStateToProps = (state: RootState): StateProps => ({
    url: state.review.currentReview.webUrl,
    description: state.review.currentReview.description
});

export default connect(
    mapStateToProps
)(reviewInfoView);