import * as React from "react";
import { connect } from 'react-redux';
import Grid from '@ui/collections/Grid';
import { RootState } from "../../rootState";
import { BuildStatus, ReviewId, ReviewMergeStatus, ReviewInfoState } from "../../api/reviewer";
import BuildStatusesList from "../../components/BuildStatusList";
import MergeApprover from './mergeApprover';
import { Dispatch } from "../../../../node_modules/redux";
import { mergePullRequest } from "./state";
import MarkdownGenerator from "./markdownGenerator"
import Actions from './sections/actions';

interface StateProps {
    reviewId: ReviewId;
    mergeStatus: ReviewMergeStatus;
    reviewState: ReviewInfoState;
    reviewFinished: boolean;
    url: string;
    description: string;
    buildStatuses: BuildStatus[];
    branches: {
        source: string;
        target: string;
    }
}

interface DispatchProps {
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string): void;
}

const reviewInfoView = (props: StateProps & DispatchProps): JSX.Element => {
    const markdown = new MarkdownGenerator();
    return (
        <Grid>
            <Grid.Row>
                <Grid.Column width={12}>
                    <MergeApprover
                        reviewId={props.reviewId}
                        mergeStatus={props.mergeStatus}
                        reviewState={props.reviewState}
                        mergePullRequest={props.mergePullRequest}
                        sourceBranch={props.branches.source}
                        targetBranch={props.branches.target}
                        reviewFinished={props.reviewFinished}
                    />
                </Grid.Column>
                <Grid.Column width={4} textAlign='left'>
                    <Actions />
                </Grid.Column>
            </Grid.Row>
            <Grid.Row>
                <Grid.Column width={12}>
                    <div dangerouslySetInnerHTML={{ __html: markdown.makeHtml(props.description) }}></div>
                </Grid.Column>
                <Grid.Column width={4}>
                    <BuildStatusesList statuses={props.buildStatuses}/>
                </Grid.Column>
            </Grid.Row>
        </Grid>
    );
}

const mapStateToProps = (state: RootState): StateProps => ({
    reviewId: state.review.currentReview.reviewId,
    reviewState: state.review.currentReview.state,
    mergeStatus: state.review.currentReview.mergeStatus,
    url: state.review.currentReview.webUrl,
    description: state.review.currentReview.description,
    buildStatuses: state.review.currentReview.buildStatuses,
    branches: {
        source: state.review.currentReview.sourceBranch,
        target: state.review.currentReview.targetBranch,
    },
    reviewFinished: state.review.currentReview.reviewFinished
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewInfoView);