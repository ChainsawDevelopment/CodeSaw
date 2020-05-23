import * as React from "react";
import { Dispatch } from "redux";
import { connect } from "react-redux";
import { History } from "history";

import Divider from '@ui/elements/Divider';
import Grid from '@ui/collections/Grid';
import Menu from '@ui/collections/Menu';
import Segment from '@ui/elements/Segment';

import {
    loadReviewInfo,
} from "./state";
import {
    ReviewInfo,
    ReviewId,
    FileId,
} from '../../api/reviewer';

import { OnMount } from "../../components/OnMount";
import { RootState } from "../../rootState";
import "./review.less";
import FileMatrix from './fileMatrix';
import ReviewInfoView from './reviewInfoView';

import CurrentReviewMode from './currentReviewMode';
import PageTitle from '../../components/PageTitle';

import Header from './sections/header';
import Actions from './sections/actions';
import ReviewDiscussions from './sections/reviewDiscussions';
import File from './sections/file';
import { useRouteMatch, Redirect, Switch, Route, useHistory, useParams } from "react-router";
import { Link } from "react-router-dom";

const LinkMenuItem = (props: {match: string; params?: any; children?: React.ReactNode}) => {
    const match = useRouteMatch(props.match);
    const parentParams = useParams();
    let to = props.match;
    if(props.params) {
        for (const key of Object.keys(props.params)) {
            to = to.replace(':' + key, props.params[key]);
        }
    }

    for (const key of Object.keys(parentParams)) {
        to = to.replace(':' + key, parentParams[key]);
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
}

interface StateProps {
    currentReview: ReviewInfo;
    selectedFileId: FileId;
    reviewMode: 'reviewer' | 'author';
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props) => {
    let { path, url } = useRouteMatch();

    const routing = {
        root: useRouteMatch(`${path}`),
        file: useRouteMatch<{fileId: string}>(`${path}file/:fileId`)
    };

    if(routing.root.isExact) {
        return <Redirect to={`${url}/matrix`}/>;
    }

    if(routing.file && props.currentReview.reviewId) {
        const fileExists = props.currentReview.filesToReview.findIndex(f => f.fileId == routing.file.params.fileId);
        if(fileExists == -1) {
            return <Redirect to={`${url}/matrix`}/>;
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
                    <ReviewDiscussions />
                </Grid>

                <div>
                    <Menu attached='top' tabular>
                        <LinkMenuItem match={`${path}matrix`}>File matrix</LinkMenuItem>
                        <LinkMenuItem match={`${path}file/:fileId`} params={{fileId: props.selectedFileId}}>Diff</LinkMenuItem>
                    </Menu>
                    <Segment attached='bottom'>
                        {props.currentReview.reviewId && <Switch>
                            <Route path={`${path}matrix`}>
                                <FileMatrix />
                            </Route>
                            <Route path={`${path}file/:fileId`}>
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
    currentReview: state.review.currentReview,
    reviewMode: state.review.currentReview.isAuthor ? 'author' : 'reviewer',
    selectedFileId: state.review.selectedFile ? state.review.selectedFile.fileId : null
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: string) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);
