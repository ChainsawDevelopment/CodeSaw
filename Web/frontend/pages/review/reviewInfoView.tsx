import * as React from "react";
import { connect } from 'react-redux';
import Grid from '@ui/collections/Grid';
import { RootState } from "../../rootState";
import { BuildStatus } from "../../api/reviewer";
import BuildStatusesList from "../../components/BuildStatusList";

interface StateProps {
    url: string;
    description: string;
    buildStatuses: BuildStatus[];
}

const reviewInfoView = (props: StateProps): JSX.Element => {
    return (
        <Grid columns={2}>
            <Grid.Row>
                <Grid.Column width={6}>
                    <a href={props.url}>Go to merge request page</a>
                </Grid.Column>
            </Grid.Row>
            <Grid.Row>
                <Grid.Column width={8}>
                    <pre>{props.description}</pre>
                </Grid.Column>
                <Grid.Column width={4}>
                    <BuildStatusesList statuses={props.buildStatuses}/>
                </Grid.Column>
            </Grid.Row>
        </Grid>
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