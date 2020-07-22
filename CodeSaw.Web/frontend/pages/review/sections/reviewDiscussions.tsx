import { connect } from 'react-redux';
import * as React from 'react';
import Grid from '@ui/collections/Grid';
import CommentsView, { DiscussionActions } from '../commentsView';
import { Discussion, ReviewInfo } from '@api/reviewer';
import { UserState, RootState } from '@src/rootState';
import { Dispatch } from 'redux';
import {
    replyToComment,
    editUnpublishedComment,
    startReviewDiscussion,
    removeUnpublishedComment,
    resolveDiscussion,
    unresolveDiscussion,
} from '../state';

interface StateProps {
    currentUser: UserState;
    currentReview: ReviewInfo;
    unpublishedReviewDiscussions: any;
    unpublishedReplies: any;
    unpublishedResolvedDiscussions: any;
}

interface DispatchProps {
    discussionActions: DiscussionActions;
}

type Props = StateProps & DispatchProps;

const ReviewDiscussion = (props: Props): JSX.Element => {
    const discussions: Discussion[] = props.currentReview.reviewDiscussions
        .concat(props.unpublishedReviewDiscussions)
        .map((d) => ({
            ...d,
            state: props.unpublishedResolvedDiscussions.indexOf(d.id) >= 0 ? 'ResolvePending' : d.state,
        }));

    return (
        <Grid.Row>
            <Grid.Column>
                <CommentsView
                    discussionId="review"
                    discussions={discussions}
                    actions={props.discussionActions}
                    unpublishedReplies={props.unpublishedReplies}
                    currentUser={props.currentUser}
                />
            </Grid.Column>
        </Grid.Row>
    );
};

export default connect(
    (state: RootState): StateProps => ({
        currentUser: state.currentUser,
        currentReview: state.review.currentReview,
        unpublishedReviewDiscussions: state.review.unpublishedReviewDiscussions,
        unpublishedResolvedDiscussions: state.review.unpublishedResolvedDiscussions,
        unpublishedReplies: state.review.unpublishedReplies,
    }),
    (dispatch: Dispatch) => ({
        addNew: (content, type, currentUser) => dispatch(startReviewDiscussion({ content, type, currentUser })),
        discussionActions: {
            addReply: (parentId, content) => dispatch(replyToComment({ parentId, content })),
            editReply: (commentId, content) => dispatch(editUnpublishedComment({ commentId, content })),
            removeUnpublishedComment: (commentId) => dispatch(removeUnpublishedComment({ commentId })),
            resolve: (discussionId) => dispatch(resolveDiscussion({ discussionId })),
            unresolve: (discussionId) => dispatch(unresolveDiscussion({ discussionId })),
        },
    }),
    (stateProps: StateProps, dispatchProps): Props => ({
        ...stateProps,
        ...dispatchProps,
        discussionActions: {
            ...dispatchProps.discussionActions,
            addNew: (content, type) => dispatchProps.addNew(content, type, stateProps.currentUser),
        },
    }),
)(ReviewDiscussion);
