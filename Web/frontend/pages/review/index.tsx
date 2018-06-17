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
    resolveComment
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
import "./review.less";
import VersionSelector from './versionSelector';
import * as PathPairs from "../../pathPair";
import ReviewSummary from './reviewSummary';
import CommentsView from './commentsView';

interface OwnProps {
    reviewId: ReviewId;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
    createGitLabLink(reviewId: ReviewId);
    publishReview(): void;
    reviewFile: ReviewFileActions;
    loadComments(reviewId: ReviewId): void;
    addComment(reviewId: ReviewId, content: string, needsResolution: boolean, parentId?: string);
    resolveComment(reviewId: ReviewId, commentId: string);
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

    const publishReview = (
        <div>
            <Button onClick={props.publishReview} color='green'>Publish</Button>
        </div>
    );

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
            <OnMount onMount={load} />

            <h1>Review {props.currentReview.title}</h1>

            <CommentsView reviewId={props.reviewId} comments={props.comments} addComment={props.addComment} resolveComment={props.resolveComment} />

            <VersionSelector
                available={['base', ...pastRevisions, ...provisional]}
                hasProvisonal={props.currentReview.hasProvisionalRevision}
                range={props.currentRange}
                onSelectRange={props.selectRevisionRange}
            />

            <ReviewSummary />

            <div>
                <Button onClick={() => props.createGitLabLink(props.reviewId)}>Create link in GitLab</Button>
            </div>
            {publishReview}
            {props.rangeInfo ? (<RangeInfo
                info={props.rangeInfo}
                selectedFile={selectedFile}
                onSelectFileForView={props.selectFileForView}
                reviewFile={props.reviewFile}
                reviewedFiles={props.reviewedFiles}
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
    createGitLabLink: (reviewId) => dispatch(createGitLabLink({ reviewId })),
    publishReview: () => dispatch(publishReview({})),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    },
    loadComments: (reviewId: ReviewId) => dispatch(loadComments({ reviewId })),
    addComment: (reviewId, content, needsResolution, parentId) => dispatch(addComment({ reviewId, content, needsResolution, parentId })),
    resolveComment: (reviewId, commentId) => dispatch(resolveComment({ reviewId, commentId }))
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);
