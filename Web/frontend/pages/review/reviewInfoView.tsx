import * as React from "react";
import { connect } from 'react-redux';
import { RootState } from "../../rootState";
import { BuildStatus } from "../../api/reviewer";
import BuildStatusesList from "../../components/BuildStatusList";

interface StateProps {
    url: string;
    description: string;
    buildStatuses: BuildStatus[];
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
            <div>
                <BuildStatusesList statuses={props.buildStatuses}/>
            </div>
        </div>
    );
}

const mapStateToProps = (state: RootState): StateProps => ({
    url: state.review.currentReview.webUrl,
    description: state.review.currentReview.description,
    buildStatuses: state.review.currentReview.buildStatuses
});

export default connect(
    mapStateToProps
)(reviewInfoView);