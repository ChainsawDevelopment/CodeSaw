import * as React from "react";

import { Dispatch } from "redux";
import {
    selectCurrentRevisions,
    selectFileForView,
    loadReviewInfo,
    FileInfo,
    publishReview,
    createGitLabLink,
    reviewFile,
    unreviewFile,
    loadComments,
    addComment,
    resolveComment,
    mergePullRequest
} from "./state";
import {
    RevisionRangeInfo,
    ReviewInfo,
    RevisionRange,
    ReviewId,
    RevisionId,
    Comment
} from '../../api/reviewer';
import Button from '@ui/elements/Button';
import { OnMount } from "../../components/OnMount";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import RangeInfo, { SelectFileForViewHandler, ReviewFileActions } from './rangeInfo';
import MergeApprover from './mergeApprover';
import "./review.less";
import VersionSelector from './versionSelector';
import * as PathPairs from "../../pathPair";
import ReviewSummary from './reviewSummary';
import CommentsView from './commentsView';

import Divider from 'semantic-ui-react/dist/commonjs/elements/Divider';
import Grid from 'semantic-ui-react/dist/commonjs/collections/Grid';

interface OwnProps {
    reviewId: ReviewId;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;    
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
    reviewFile: ReviewFileActions;
    loadComments(reviewId: ReviewId): void;
    addComment(reviewId: ReviewId, content: string, needsResolution: boolean, parentId?: string);
    resolveComment(reviewId: ReviewId, commentId: string);
    publishReview(): void;
}

interface StateProps {
    currentReview: ReviewInfo;
    currentRange: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: FileInfo;
    reviewedFiles: PathPairs.List;
    comments: Comment[];
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props): JSX.Element => {
    const provisional: RevisionId[] = props.currentReview.hasProvisionalRevision ? ['provisional'] : [];

    const pastRevisions = props.currentReview.pastRevisions.map(i => i.number);

    const selectedFile = props.selectedFile ?
        {...props.selectedFile, isReviewed: PathPairs.contains(props.reviewedFiles, props.selectedFile.path)}
        : null;

    const load = () => {
        props.loadReviewInfo(props.reviewId);
        props.loadComments(props.reviewId);
    };

    return (
        <div id="review-page">
            <Grid centered columns={2}>
                <Grid.Row>
                    <Grid.Column>
        
                    <OnMount onMount={load} />

                    <h1>Review {props.currentReview.title}</h1>

                    <MergeApprover
                        reviewId={props.reviewId}
                        mergePullRequest={props.mergePullRequest}
                    />
                    <Divider />
                    
                    <VersionSelector
                        available={['base', ...pastRevisions, ...provisional]}
                        hasProvisonal={props.currentReview.hasProvisionalRevision}
                        range={props.currentRange}
                        onSelectRange={props.selectRevisionRange}
                    />

                    <ReviewSummary 
                        onSelectFileForView={props.selectFileForView} />

                    <CommentsView reviewId={props.reviewId} comments={props.comments} addComment={props.addComment} resolveComment={props.resolveComment} />

                    </Grid.Column>
                </Grid.Row>
            </Grid>

            <Divider />

            {props.rangeInfo ? (<RangeInfo
                info={props.rangeInfo}
                selectedFile={selectedFile}
                onSelectFileForView={props.selectFileForView}
                reviewFile={props.reviewFile}
                reviewedFiles={props.reviewedFiles}
                publishReview={props.publishReview}
            />) : null}
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentReview: state.review.currentReview,
    currentRange: state.review.range,
    rangeInfo: state.review.rangeInfo,
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles,
    comments: state.review.comments
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId) => dispatch(loadReviewInfo({ reviewId })),
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: (path) => dispatch(selectFileForView({ path })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    },
    loadComments: (reviewId: ReviewId) => dispatch(loadComments({ reviewId })),
    addComment: (reviewId, content, needsResolution, parentId) => dispatch(addComment({ reviewId, content, needsResolution, parentId })),
    resolveComment: (reviewId, commentId) => dispatch(resolveComment({ reviewId, commentId })),
    publishReview: () => dispatch(publishReview({})),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);
