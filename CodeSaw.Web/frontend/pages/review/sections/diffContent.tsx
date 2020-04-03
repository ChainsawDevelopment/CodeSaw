import { connect } from "react-redux";
import * as React from "react";
import { FileView } from "./fileView";
import { Dispatch } from "redux";
import { RootState, UserState } from "@src/rootState";
import { FileInfo, startFileDiscussion, startReviewDiscussion, replyToComment, editUnpublishedComment, removeUnpublishedComment, resolveDiscussion, unresolveDiscussion } from "../state";
import { DiscussionActions } from "../commentsView";
import { FileId, FileDiscussion, CommentReply } from "@api/reviewer";

interface OwnProps {
    scrollToFile(): void;
}

interface StateProps {
    selectedFile: FileInfo;
    unpublishedFileDiscussion: FileDiscussion[];
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    currentUser: UserState;
}

interface DispatchProps {
    commentActions: DiscussionActions;
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean): void;
}

type Props = OwnProps & StateProps & DispatchProps;

const DiffContent = (props: Props): JSX.Element => {
    return <FileView
        file={props.selectedFile}
        commentActions={props.commentActions}
        comments={props.selectedFile.discussions}
        startFileDiscussion={props.startFileDiscussion}
        unpublishedFileDiscussions={props.unpublishedFileDiscussion}
        pendingResolved={props.pendingResolved}
        unpublishedReplies={props.unpublishedReplies}
        currentUser={props.currentUser}
        scrollToFile={props.scrollToFile}
    />;
};

export default connect(
    (state: RootState): StateProps => ({
        selectedFile: state.review.selectedFile,
        unpublishedFileDiscussion: state.review.unpublishedFileDiscussions,
        pendingResolved: state.review.unpublishedResolvedDiscussions,
        unpublishedReplies: state.review.unpublishedReplies,
        currentUser: state.currentUser,
    }),
    (dispatch: Dispatch) => ({
        startFileDiscussion: (fileId, lineNumber, content, needsResolution, currentUser) => dispatch(startFileDiscussion({ fileId, lineNumber, content, needsResolution, currentUser })),
        addNew: (content, needsResolution, currentUser) => dispatch(startReviewDiscussion({ content, needsResolution, currentUser })),
        commentActions: {
            addReply: (parentId, content) => dispatch(replyToComment({ parentId, content })),
            editReply: (commentId, content) => dispatch(editUnpublishedComment({ commentId, content })),
            removeUnpublishedComment: (commentId) => dispatch(removeUnpublishedComment({ commentId })),
            resolve: (discussionId) => dispatch(resolveDiscussion({ discussionId })),
            unresolve: (discussionId) => dispatch(unresolveDiscussion({ discussionId })),
        }
    }),
    (stateProps: StateProps, dispatchProps, ownProps: OwnProps): Props => ({
        ...stateProps,
        ...dispatchProps,
        ...ownProps,
        startFileDiscussion: (path, lineNumber, content, needsResolution) => dispatchProps.startFileDiscussion(path, lineNumber, content, needsResolution, stateProps.currentUser),
        commentActions: {
            ...dispatchProps.commentActions,
            addNew: (content, needsResolution) => dispatchProps.addNew(content, needsResolution, stateProps.currentUser),
        },
    })
)(DiffContent);