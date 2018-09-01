import * as React from "react";
import Button from '@ui/elements/Button';
import Checkbox, { CheckboxProps } from '@ui/modules/Checkbox';
import UIComment from '@ui/views/Comment';
import Form from '@ui/collections/Form';
import Header from '@ui/elements/Header';
import { TextAreaProps } from '@ui/addons/TextArea';
import { Comment, Discussion } from "../../api/reviewer";

import "./commentsView.less";
import { UserState } from "../../rootState";

export interface DiscussionActions {
    addNew(content: string, needsResolution: boolean);
    addReply(parentId: string, content: string):void;
    resolve(discussionId: string);
    unresolve(discussionId: string);
}

interface Reply {
    id: string;
    parentId: string;
    content: string;
}

interface CommentsState {
    commentText: string;
    needsResolution: boolean;
}

interface CommentProps {
    comment: Comment;
    actions: DiscussionActions;
    statusComponent?: JSX.Element;
}

interface CommentState {
    replyText: string;
    replyVisible: boolean;
}

const mapComments = (
    comments: Comment[],
    actions: DiscussionActions
): JSX.Element[] => {
    return comments.map(comment => (
        <CommentComponent
            key={comment.id}
            comment={comment}
            actions={actions}
        />
    ));
};

class CommentComponent extends React.Component<CommentProps, CommentState> {
    constructor(props: CommentProps) {
        super(props);

        this.state = {
            replyText: '',
            replyVisible: false
        };
    }

    render(): JSX.Element {
        const children = mapComments(this.props.comment.children, this.props.actions);

        const switchReply = () => {
            this.setState({ replyVisible: !this.state.replyVisible });
        };

        const onSubmit = () => {
            this.setState({ replyText: '', replyVisible: false });
        };

        const onChangeReply = (event: React.FormEvent<HTMLTextAreaElement>, data: TextAreaProps) => {
            this.setState({ replyText: data.value.toString() });
        };

        const form = (
            <Form reply onSubmit={onSubmit}>
                <Form.TextArea onChange={onChangeReply} value={this.state.replyText} />
                <Button onClick={() => this.props.actions.addReply(this.props.comment.id, this.state.replyText)} primary>Add Comment</Button>
            </Form>
        );

        return (
            <UIComment>
                <UIComment.Avatar src={this.props.comment.author.avatarUrl} />
                <UIComment.Content>
                    <UIComment.Author>{this.props.comment.author.givenName}</UIComment.Author>
                    <UIComment.Metadata>
                        <div>{this.props.comment.createdAt}</div>
                    </UIComment.Metadata>
                    <UIComment.Text>{this.props.comment.content}</UIComment.Text>
                    <UIComment.Actions>
                        <UIComment.Action active={this.state.replyVisible} onClick={switchReply}>Reply</UIComment.Action>
                        {this.props.statusComponent}
                    </UIComment.Actions>
                    {this.state.replyVisible ? form : null}
                    {children}
                </UIComment.Content>
            </UIComment>
        )
    }
}

interface DiscussionComponentProps {
    discussion: Discussion;
    actions: DiscussionActions;
}
const DiscussionComponent = (props: DiscussionComponentProps) => {
    let status: JSX.Element = null;
    switch (props.discussion.state) {
        case 'NoActionNeeded':
            status = null;
            break;
        case 'NeedsResolution':
            const resolve = () => props.actions.resolve(props.discussion.id);
            status =  <UIComment.Action onClick={resolve}>Resolve</UIComment.Action>;
            break;
        case 'ResolvePending':
            const unresolve = () => props.actions.unresolve(props.discussion.id);
            status =  <UIComment.Action className="resolved-pending" onClick={unresolve}>Resolved (pending)</UIComment.Action>;
            break;
        case 'Resolved':
            status =  <span>Resolved</span>;
            break;
    }

    return (<CommentComponent 
        comment={props.discussion.comment}
        statusComponent={status}
        actions={props.actions}
    />);
};

const mergeCommentsWithReplies = (
    comments: Comment[],
    replies:  Reply[],
    currentUser: UserState
): Comment[] => {
    const result: Comment[] = [];

    const replyToComment = (reply: Reply): Comment => ({
        id: reply.id,
        author: currentUser,
        content: reply.content,
        createdAt: '',
        children: [] as Comment[]
    });

    for (let item of comments) {
        result.push({
            ...item,
            children: mergeCommentsWithReplies([
                ...item.children,
                ...replies.filter(r => r.parentId == item.id).map(replyToComment)
            ], replies, currentUser)
        });
    }

    return result;
}

interface DiscussionsProps {
    discussions: Discussion[];
    unpublishedReplies: Reply[];
    actions: DiscussionActions;
    currentUser: UserState;
}

export default class CommentsComponent extends React.Component<DiscussionsProps, CommentsState> {
    constructor(props: DiscussionsProps) {
        super(props);

        this.state = {
            commentText: '',
            needsResolution: false
        };
    }

    render(): JSX.Element {
        const discussionsWithReplies = 
            this.props.discussions.map(d=>({
                ...d,
                comment: mergeCommentsWithReplies([d.comment], this.props.unpublishedReplies, this.props.currentUser)[0]
            }))

        const discussions = discussionsWithReplies.map(d => <DiscussionComponent key={d.id} discussion={d} actions={this.props.actions}/>)

        const onSubmit = () => {
            this.setState({ commentText: '' });
        };

        const onChangeReply = (event: React.FormEvent<HTMLTextAreaElement>, data: TextAreaProps) => {
            this.setState({ commentText: data.value.toString() });
        };

        const onChangeNeedsResolution = (event: React.FormEvent<HTMLInputElement>, data: CheckboxProps) => {
            this.setState({ needsResolution: data.checked });
        };

        return (
            <UIComment.Group>
                <Header as='h3' dividing>
                    Comments
                </Header>
                {discussions}
                <Form reply onSubmit={onSubmit}>
                    <Form.TextArea onChange={onChangeReply} value={this.state.commentText} />
                    <Button onClick={() => this.props.actions.addNew(this.state.commentText, this.state.needsResolution)} secondary>Add Comment</Button>
                    <Checkbox onChange={onChangeNeedsResolution} checked={this.state.needsResolution} label="Needs resolution" />
                </Form>
            </UIComment.Group>
        );
    }
}
