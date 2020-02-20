import * as React from "react";
import { Dispatch } from "redux";
import { connect } from "react-redux";
import { History } from "history";

import { PublishButton } from "./PublishButton";

import Menu from '@ui/collections/Menu';
import Divider from '@ui/elements/Divider';
import Grid from '@ui/collections/Grid';
import Checkbox, { CheckboxProps } from '@ui/modules/Checkbox';

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
    replyToComment,
    markEmptyFilesAsReviewed,
    editUnpublishedComment
} from "./state";
import {
    ReviewInfo,
    ReviewId,
    FileDiscussion,
    ReviewDiscussion,
    CommentReply,
    Discussion,
    FileId,
} from '../../api/reviewer';

import { OnMount } from "../../components/OnMount";
import { OnPropChanged } from "../../components/OnPropChanged";
import { UserState, RootState } from "../../rootState";
import RangeInfo, { SelectFileForViewHandler, ReviewFileActions } from './rangeInfo';
import "./review.less";
import CommentsView, { DiscussionActions } from './commentsView';
import FileMatrix from './fileMatrix';
import ReviewInfoView from './reviewInfoView';
import UserInfo from "../../components/UserInfo";

import ExternalLink from "../../components/externalLink";
import { createLinkToFile } from "./FileLink";
import CurrentReviewMode from './currentReviewMode';
import PageTitle from '../../components/PageTitle';
import { Button } from "semantic-ui-react";

interface OwnProps {
    reviewId: ReviewId;
    fileId?: FileId;
    history: History;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId, fileToPreload?: FileId): void;
    selectFileForView: SelectFileForViewHandler;
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
    reviewFile: ReviewFileActions;
    publishReview(): void;
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean, currentUser?: UserState): void;
    startReviewDiscussion(content: string, needsResolution: boolean, currentUser?: UserState): void;
    resolveDiscussion(rootCommentId: string): void;
    unresolveDiscussion(rootCommentId: string): void;
    addReply(parentCommentId: string, content: string): void;
    editReply(commentId: string, content: string): void;
    markNonEmptyAsViewed(): void;
}

interface StateProps {
    currentUser: UserState;
    currentReview: ReviewInfo;
    selectedFile: FileInfo;
    reviewedFiles: FileId[];
    unpublishedFileDiscussion: FileDiscussion[];
    unpublishedReviewDiscussions: ReviewDiscussion[];
    unpublishedResolvedDiscussions: string[];
    unpublishedReplies: CommentReply[];
    author: UserState,
    reviewMode: 'reviewer' | 'author';
}

type Props = OwnProps & StateProps & DispatchProps;

interface State {
    hideReviewed: boolean;
}

class reviewPage extends React.Component<Props, State> {
    private showFileHandler: () => void;

    constructor(props: Props) {
        super(props);
        this.state = {
            hideReviewed: false
        };
    }

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
            { ...props.selectedFile, isReviewed: props.reviewedFiles.indexOf(props.selectedFile.fileId) >= 0 }
            : null;

        const load = () => {
            if (!selectedFile && props.fileId) {
                this.onShowFileHandlerAvailable = this.scrollToFileWhenHandlerIsAvailable;
                props.loadReviewInfo(props.reviewId, props.fileId);
            } else {
                this.onShowFileHandlerAvailable = this.saveShowFileHandler;
                props.loadReviewInfo(props.reviewId, );
            }
        };

        const selectNewFileForView = (fileId: FileId) => {
            if (fileId != null) {
                props.selectFileForView(fileId);
                
                const fileLink = createLinkToFile(props.reviewId, fileId);
                if (fileLink != window.location.pathname) {
                    props.history.push(fileLink);
                }

                this.onShowFile();
            }
        };

        const selectFileForView = () => {
            const file = props.currentReview.filesToReview.find(f => f.fileId == props.fileId);
            if (file != null) {
                selectNewFileForView(file.fileId);
            }
        };

        const commentActions: DiscussionActions = {
            addNew: (content, needsResolution) => props.startReviewDiscussion(content, needsResolution),
            addReply: props.addReply,
            editReply: props.editReply,
            resolve: props.resolveDiscussion,
            unresolve: props.unresolveDiscussion
        }

        const discussions: Discussion[] = props.currentReview.reviewDiscussions
            .concat(props.unpublishedReviewDiscussions)
            .map(d => ({
                ...d,
                state: props.unpublishedResolvedDiscussions.indexOf(d.id) >= 0 ? 'ResolvePending' : d.state
            }));

        const title = (() => {
            if (!props.currentReview.reviewId) {
                return 'Loading review...';
            }

            const { currentReview } = props;
            return `[${currentReview.projectPath}] #${currentReview.reviewId.reviewId} - ${currentReview.title}`;
        })();

        const changeHideReviewed = (e: React.SyntheticEvent, data: CheckboxProps) => {
            this.setState({
                hideReviewed: data.checked
            });
        };


        return (
            <div id="review-page">
                <PageTitle>{title}</PageTitle>
                <CurrentReviewMode.Provider value={props.reviewMode}>
                    <OnMount onMount={load} />
                    <OnPropChanged fileName={props.fileId} onPropChanged={selectFileForView} />

                    <Grid>
                        <Grid.Row>
                            <Grid.Column className={"header"}>
                                <Grid.Row>
                                    <h1>Review {props.currentReview.title} <ExternalLink url={props.currentReview.webUrl} /></h1>
                                    <h3>{props.currentReview.projectPath}</h3>
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
                                <Menu secondary id="summary-menu">
                                    <Menu.Menu position='right'>
                                        <Menu.Item>
                                            <PublishButton />&nbsp;
                                            <Button onClick={props.markNonEmptyAsViewed}>Mark Unchanged Files</Button>&nbsp;
                                            <Checkbox toggle label="Hide reviewed" onChange={changeHideReviewed} />&nbsp;

                                        </Menu.Item>
                                    </Menu.Menu>
                                </Menu>
                                
                                <FileMatrix hideReviewed={this.state.hideReviewed}/>
                            </Grid.Column>
                        </Grid.Row>
                        <Grid.Row>
                            <Grid.Column>
                                <CommentsView
                                    discussionId="review"
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
                        onSelectFileForView={selectNewFileForView}
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
                        markNonEmptyAsViewed={props.markNonEmptyAsViewed}
                    />
                </CurrentReviewMode.Provider>
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
    author: state.review.currentReview.author,
    unpublishedReplies: state.review.unpublishedReplies,
    reviewMode: state.review.currentReview.isAuthor ? 'author' : 'reviewer'
});

const mapDispatchToProps = (dispatch: Dispatch, ownProps: OwnProps): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: string) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
    selectFileForView: (fileId) => dispatch(selectFileForView({ fileId })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    },
    publishReview: () => dispatch(publishReview({ fileToLoad: ownProps.fileId })),
    startFileDiscussion: (fileId, lineNumber, content, needsResolution, currentUser) => dispatch(startFileDiscussion({ fileId, lineNumber, content, needsResolution, currentUser })),
    startReviewDiscussion: (content, needsResolution, currentUser) => dispatch(startReviewDiscussion({ content, needsResolution, currentUser })),
    resolveDiscussion: (discussionId) => dispatch(resolveDiscussion({ discussionId })),
    unresolveDiscussion: (discussionId) => dispatch(unresolveDiscussion({ discussionId })),
    addReply: (parentId, content) => dispatch(replyToComment({ parentId, content })),
    editReply: (commentId, content) => dispatch(editUnpublishedComment({commentId, content})),
    markNonEmptyAsViewed: () => dispatch(markEmptyFilesAsReviewed({})),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps,
    (stateProps: StateProps, dispatchProps: DispatchProps, ownProps: OwnProps) : Props => ({
        ...ownProps,
        ...stateProps,
        ...dispatchProps,
        startFileDiscussion: (path, lineNumber, content, needsResolution) => dispatchProps.startFileDiscussion(path, lineNumber, content, needsResolution, stateProps.currentUser),
        startReviewDiscussion: (content, needsResolution) => dispatchProps.startReviewDiscussion(content, needsResolution, stateProps.currentUser)
    })
)(reviewPage);
