import * as React from "react";
import { Dispatch } from "redux";
import { connect } from "react-redux";
import { History } from "history";

import Divider from '@ui/elements/Divider';
import Grid from '@ui/collections/Grid';
import Tab from '@ui/modules/Tab';
import Menu from '@ui/collections/Menu';
import Segment from '@ui/elements/Segment';

import {
    selectFileForView,
    loadReviewInfo,
    FileInfo,
    mergePullRequest,
} from "./state";
import {
    ReviewInfo,
    ReviewId,
    FileId,
} from '../../api/reviewer';

import { OnMount } from "../../components/OnMount";
import { UserState, RootState } from "../../rootState";
import { SelectFileForViewHandler } from './selectFile';
import "./review.less";
import FileMatrix from './fileMatrix';
import ReviewInfoView from './reviewInfoView';

import { createLinkToFile } from "./FileLink";
import CurrentReviewMode from './currentReviewMode';
import PageTitle from '../../components/PageTitle';

import Header from './sections/header';
import Actions from './sections/actions';
import ReviewDiscussions from './sections/reviewDiscussions';
import File from './sections/file';
import { useRouteMatch, Redirect, Switch, Route, withRouter, useHistory } from "react-router";
import { Link } from "react-router-dom";

const LinkMenuItem = (props: {match: string; params?: any; children?: React.ReactNode}) => {
    const match = useRouteMatch(props.match);
    let to = props.match;
    if(props.params) {
        for (const key of Object.keys(props.params)) {
            to = to.replace(':' + key, props.params[key]);
        }
    }
    return <Menu.Item as={Link} to={to} active={match != null}>
        {props.children}
    </Menu.Item>
}

const RoutedFile = (props: {reviewId: ReviewId}): JSX.Element => {
    const match = useRouteMatch<{fileId: FileId}>('/project/:projectId/review/:reviewId/file/:fileId');
    const history = useHistory();

    return <File
        reviewId={props.reviewId}
        fileId={match.params.fileId}
        history={history}
    />;
}

interface OwnProps {
    reviewId: ReviewId;
    history: History;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId, fileToPreload?: FileId): void;
    selectFileForView: SelectFileForViewHandler;
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string): void;
}

interface StateProps {
    currentUser: UserState;
    currentReview: ReviewInfo;
    selectedFile: FileInfo;
    reviewedFiles: FileId[];
    author: UserState;
    reviewMode: 'reviewer' | 'author';
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props) => {
    const routing = {
        root: useRouteMatch('/project/:projectId/review/:reviewId/'),
        file: useRouteMatch<{fileId: string}>('/project/:projectId/review/:reviewId/file/:fileId')
    };

    if(routing.root.isExact) {
        return <Redirect to={`/project/${props.reviewId.projectId}/review/${props.reviewId.reviewId}/matrix`}/>;
    }

    if(routing.file && props.currentReview.reviewId) {
        const fileExists = props.currentReview.filesToReview.findIndex(f => f.fileId == routing.file.params.fileId);
        if(fileExists == -1) {
            return <Redirect to={`/project/${props.reviewId.projectId}/review/${props.reviewId.reviewId}/matrix`}/>;
        }
    }

    const load = () => props.loadReviewInfo(props.reviewId);

    const title = (() => {
        if (!props.currentReview.reviewId) {
            return 'Loading review...';
        }

        const { currentReview } = props;
        return `[${currentReview.projectPath}] #${currentReview.reviewId.reviewId} - ${currentReview.title}`;
    })();

    return (
        <div id="review-page">
            <PageTitle>{title}</PageTitle>
            <CurrentReviewMode.Provider value={props.reviewMode}>
                <OnMount onMount={load} />

                <Grid>
                    <Header />

                    <Grid.Row>
                        <Grid.Column>
                            <ReviewInfoView />
                            <Divider />
                        </Grid.Column>
                    </Grid.Row>
                    <Grid.Row columns={1}>
                        <Grid.Column>
                            <Actions onHideReviewedChange={null} />
                        </Grid.Column>
                    </Grid.Row>
                    <ReviewDiscussions />
                </Grid>

                <Divider />

                <div>
                    <Menu attached='top' tabular>
                        <LinkMenuItem match={`/project/${props.reviewId.projectId}/review/${props.reviewId.reviewId}/matrix`}>File matrix</LinkMenuItem>
                        <LinkMenuItem match={`/project/${props.reviewId.projectId}/review/${props.reviewId.reviewId}/file/:fileId`} params={{fileId: 'abc'}}>Diff</LinkMenuItem>
                    </Menu>
                    <Segment attached='bottom'>
                        {props.currentReview.reviewId && <Switch>
                            <Route path="/project/:projectId/review/:reviewId/matrix">
                                <FileMatrix
                                    hideReviewed={false}
                                />
                            </Route>
                            <Route path="/project/:projectId/review/:reviewId/file/:fileId">
                                <RoutedFile reviewId={props.reviewId}/>
                            </Route>
                        </Switch>}
                    </Segment>
                </div>
            </CurrentReviewMode.Provider>
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentUser: state.currentUser,
    currentReview: state.review.currentReview,
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles,
    author: state.review.currentReview.author,
    reviewMode: state.review.currentReview.isAuthor ? 'author' : 'reviewer',
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: string) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
    selectFileForView: (fileId) => dispatch(selectFileForView({ fileId })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);
