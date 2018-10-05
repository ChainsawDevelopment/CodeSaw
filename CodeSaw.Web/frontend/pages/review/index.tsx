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
    ReviewInfo,
    ReviewId,
    Comment,
    FileDiscussion,
    ReviewDiscussion,
    CommentReply,
    FileToReview,
    Discussion,
} from '../../api/reviewer';
import { OnMount } from "../../components/OnMount";
import { OnPropChanged } from "../../components/OnPropChanged";
import { connect } from "react-redux";
import { UserState, RootState } from "../../rootState";
import RangeInfo, { SelectFileForViewHandler, ReviewFileActions } from './rangeInfo';
import MergeApprover from './mergeApprover';
import "./review.less";
import * as PathPairs from "../../pathPair";
import CommentsView, { DiscussionActions } from './commentsView';
import FileMatrix from './fileMatrix';
import ReviewInfoView from './reviewInfoView';
import UserInfo from "../../components/UserInfo";

import Divider from 'semantic-ui-react/dist/commonjs/elements/Divider';
import Grid from 'semantic-ui-react/dist/commonjs/collections/Grid';
import Icon from '@ui/elements/Icon';
import ExternalLink from "../../components/externalLink";

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
    selectedFile: FileInfo;
    reviewedFiles: PathPairs.List;
    unpublishedFileDiscussion: FileDiscussion[];
    unpublishedReviewDiscussions: ReviewDiscussion[];
    unpublishedResolvedDiscussions: string[];
    unpublishedReplies: CommentReply[];
    author: UserState,
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
            const file = props.currentReview.filesToReview.find(f => f.reviewFile.newPath == props.fileName);
            if (file != null) {
                props.selectFileForView(file.reviewFile);
                this.onShowFile();
            }
        };

        const commentActions: DiscussionActions = {
            addNew: (content, needsResolution) => props.startReviewDiscussion(content, needsResolution),
            addReply: props.addReply,
            resolve: props.resolveDiscussion,
            unresolve: props.unresolveDiscussion
        }

        const discussions: Discussion[] = props.currentReview.reviewDiscussions
            .concat(props.unpublishedReviewDiscussions)
            .map(d => ({
                ...d,
                state: props.unpublishedResolvedDiscussions.indexOf(d.id) >= 0 ? 'ResolvePending' : d.state
            }));

        return (
            <div id="review-page">
                <OnMount onMount={load} />
                <OnPropChanged fileName={props.fileName} onPropChanged={selectFileForView} />
                
                <Grid>
                    <Grid.Row>
                        <Grid.Column>
                            <Grid.Row>
                                <h1>Review {props.currentReview.title} <ExternalLink url={props.currentReview.webUrl} /></h1>
                            </Grid.Row>
                            <Grid.Row>
                                <UserInfo
                                    username={props.author.username}
                                    name={props.author.name}
                                    avatarUrl={props.author.avatarUrl}
                                />
                            </Grid.Row>
                        </Grid.Column>
                    </Grid.Row>
                    <Grid.Row>
                        <Grid.Column>
                            <ReviewInfoView />
                            <Divider />
                        </Grid.Column>
                    </Grid.Row>
                    <Grid.Row centered columns={1}>
                        <Grid.Column>
                            <FileMatrix />
                        </Grid.Column>
                    </Grid.Row>
                    <Grid.Row>
                        <Grid.Column>
                            <CommentsView
                                discussions={discussions}
                                actions={commentActions}
                                unpublishedReplies={props.unpublishedReplies}
                                currentUser={props.currentUser}
                            />
                        </Grid.Column>
                    </Grid.Row>
                </Grid>

                <Divider />

                <RangeInfo
                    filesToReview={props.currentReview.filesToReview}
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
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles,
    unpublishedFileDiscussion: state.review.unpublishedFileDiscussions,
    unpublishedReviewDiscussions: state.review.unpublishedReviewDiscussions,
    unpublishedResolvedDiscussions: state.review.unpublishedResolvedDiscussions,
    unpublishedReplies: state.review.unpublishedReplies,
    author: state.review.currentReview.author,
});

const mapDispatchToProps = (dispatch: Dispatch, ownProps: OwnProps): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: string) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
    selectFileForView: (path) => dispatch(selectFileForView({ path })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    },
    publishReview: () => dispatch(publishReview({ fileToLoad: ownProps.fileName })),
    startFileDiscussion: (path, lineNumber, content, needsResolution, currentUser) => dispatch(startFileDiscussion({ path, lineNumber, content, needsResolution, currentUser })),
    startReviewDiscussion: (content, needsResolution, currentUser) => dispatch(startReviewDiscussion({ content, needsResolution, currentUser })),
    resolveDiscussion: (discussionId) => dispatch(resolveDiscussion({ discussionId })),
    unresolveDiscussion: (discussionId) => dispatch(unresolveDiscussion({ discussionId })),
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
