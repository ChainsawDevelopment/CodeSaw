import * as React from "react";

import { Dispatch } from "redux";
import {
    selectFileForView,
    loadReviewInfo,
    FileInfo,
    publishReview,
    reviewFile,
    unreviewFile,
    mergePullRequest,
    startFileDiscussion,
    startReviewDiscussion,
    resolveDiscussion,
    unresolveDiscussion,
    replyToComment
} from "./state";
import {
    RevisionRangeInfo,
    ReviewInfo,
    ReviewId,
    Comment,
    FileDiscussion,
    ReviewDiscussion,
    CommentReply,
    FileToReview
} from '../../api/reviewer';
import { OnMount } from "../../components/OnMount";
import { OnPropChanged } from "../../components/OnPropChanged";
import { connect } from "react-redux";
import { UserState, RootState } from "../../rootState";
import RangeInfo, { SelectFileForViewHandler, ReviewFileActions } from './rangeInfo';
import MergeApprover from './mergeApprover';
import "./review.less";
import * as PathPairs from "../../pathPair";
import ReviewSummary from './reviewSummary';
import CommentsView, { CommentsActions } from './commentsView';
import FilesToReview from './filesToReview';

import Divider from 'semantic-ui-react/dist/commonjs/elements/Divider';
import Grid from 'semantic-ui-react/dist/commonjs/collections/Grid';

interface OwnProps {
    reviewId: ReviewId;
    fileName?: string;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId, fileToPreload?: string): void;
    selectFileForView: SelectFileForViewHandler;
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
    reviewFile: ReviewFileActions;
    publishReview(): void;
    startFileDiscussion(path: PathPairs.PathPair, lineNumber: number, content: string, needsResolution: boolean, currentUser?: UserState): void;
    startReviewDiscussion(content: string, needsResolution: boolean, currentUser?: UserState): void;
    resolveDiscussion(rootCommentId: string): void;
    unresolveDiscussion(rootCommentId: string): void;
    addReply(parentCommentId: string, content: string): void;
}

interface StateProps {
    currentUser: UserState;
    currentReview: ReviewInfo;
    filesToReview: FileToReview[];
    selectedFile: FileInfo;
    reviewedFiles: PathPairs.List;
    unpublishedFileDiscussion: FileDiscussion[];
    unpublishedReviewDiscussions: ReviewDiscussion[];
    unpublishedResolvedDiscussions: string[];
    unpublishedReplies: CommentReply[];
}

type Props = OwnProps & StateProps & DispatchProps;

class reviewPage extends React.Component<Props> {
    private showFileHandler: () => void;

    onShowFile() {
        if (this.showFileHandler) {
            this.showFileHandler()
        }
    }

    saveShowFileHandler = (showFileHandler: () => void) => {
        this.showFileHandler = showFileHandler;
    }

    scrollToFileWhenHandlerIsAvailable = (showFileHandler: () => void) => {
        this.saveShowFileHandler(showFileHandler);
        showFileHandler();
    }

    onShowFileHandlerAvailable = this.saveShowFileHandler;

    render() {
        const props = this.props;

        const selectedFile = props.selectedFile ?
            { ...props.selectedFile, isReviewed: PathPairs.contains(props.reviewedFiles, props.selectedFile.path) }
            : null;

        const load = () => {
            if (!selectedFile && props.fileName) {
                this.onShowFileHandlerAvailable = this.scrollToFileWhenHandlerIsAvailable;
                props.loadReviewInfo(props.reviewId, props.fileName);
            } else {
                this.onShowFileHandlerAvailable = this.saveShowFileHandler;
                props.loadReviewInfo(props.reviewId, );
            }
        };

        const selectFileForView = () => {
            const fullPath = props.filesToReview.find(f => f.path.newPath == props.fileName).path;

            props.selectFileForView(fullPath);
            this.onShowFile();
        };

        const commentActions: CommentsActions = {
            addNew: (content, needsResolution) => props.startReviewDiscussion(content, needsResolution),
            addReply: props.addReply,
            resolve: props.resolveDiscussion,
            unresolve: props.unresolveDiscussion
        }

        const comments: Comment[] = props.currentReview.reviewDiscussions
            .concat(props.unpublishedReviewDiscussions)
            .map(d => ({
                ...d.comment,
                state: props.unpublishedResolvedDiscussions.indexOf(d.comment.id) >= 0 ? 'ResolvePending' : d.comment.state
            }));

        return (
            <div id="review-page">
                <Grid centered columns={2}>
                    <Grid.Row>
                        <Grid.Column>

                            <OnMount onMount={load} />
                            <OnPropChanged fileName={props.fileName} onPropChanged={selectFileForView} />

                            <h1>Review {props.currentReview.title}</h1>

                            <MergeApprover
                                reviewId={props.reviewId}
                                reviewState={props.currentReview.state}
                                mergePullRequest={props.mergePullRequest}
                            />
                            <Divider />

                            <Grid columns={2}>
                                <Grid.Row>
                                    <Grid.Column>
                                        <ReviewSummary
                                            reviewId={props.reviewId}
                                        />
                                    </Grid.Column>
                                    <Grid.Column>
                                        <FilesToReview />
                                    </Grid.Column>
                                </Grid.Row>
                            </Grid>

                            <CommentsView 
                                comments={comments} 
                                actions={commentActions} 
                                unpublishedReplies={props.unpublishedReplies} 
                                currentUser={props.currentUser}
                            />

                        </Grid.Column>
                    </Grid.Row>
                </Grid>

                <Divider />

                <RangeInfo
                    filesToReview={props.filesToReview}
                    selectedFile={selectedFile}
                    onSelectFileForView={props.selectFileForView}
                    reviewFile={props.reviewFile}
                    reviewedFiles={props.reviewedFiles}
                    publishReview={props.publishReview}
                    onShowFileHandlerAvailable={this.onShowFileHandlerAvailable}
                    reviewId={props.reviewId}
                    fileComments={props.currentReview.fileDiscussions}
                    startFileDiscussion={props.startFileDiscussion}
                    unpublishedFileDiscussion={props.unpublishedFileDiscussion}
                    commentActions={commentActions}
                    pendingResolved={props.unpublishedResolvedDiscussions}
                    unpublishedReplies={props.unpublishedReplies}
                    currentUser={props.currentUser}
                />
            </div>
        );
    }
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentUser: state.currentUser,
    currentReview: state.review.currentReview,
    filesToReview: state.review.filesToReview,
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles,
    unpublishedFileDiscussion: state.review.unpublishedFileDiscussions,
    unpublishedReviewDiscussions: state.review.unpublishedReviewDiscussions,
    unpublishedResolvedDiscussions: state.review.unpublishedResolvedDiscussions,
    unpublishedReplies: state.review.unpublishedReplies
});

const mapDispatchToProps = (dispatch: Dispatch, ownProps: OwnProps): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: string) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
    selectFileForView: (path) => dispatch(selectFileForView({ path })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    },
    publishReview: () => dispatch(publishReview({fileToLoad: ownProps.fileName})),
    startFileDiscussion: (path, lineNumber, content, needsResolution, currentUser) => dispatch(startFileDiscussion({ path, lineNumber, content, needsResolution, currentUser })),
    startReviewDiscussion: (content, needsResolution, currentUser) => dispatch(startReviewDiscussion({ content, needsResolution, currentUser })),
    resolveDiscussion: (rootCommentId) => dispatch(resolveDiscussion({rootCommentId})),
    unresolveDiscussion: (rootCommentId) => dispatch(unresolveDiscussion({rootCommentId})),
    addReply: (parentId, content) => dispatch(replyToComment({ parentId, content })),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps,
    (stateProps, dispatchProps, ownProps) => ({
        ...ownProps,
        ...stateProps,
        ...dispatchProps,
        startFileDiscussion: (path, lineNumber, content, needsResolution) => dispatchProps.startFileDiscussion(path, lineNumber, content, needsResolution, stateProps.currentUser),
        startReviewDiscussion: (content, needsResolution) => dispatchProps.startReviewDiscussion(content, needsResolution, stateProps.currentUser)
    })
)(reviewPage);
