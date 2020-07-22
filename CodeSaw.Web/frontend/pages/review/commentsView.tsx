import * as React from 'react';
import Button from '@ui/elements/Button';
import UIComment from '@ui/views/Comment';
import Form from '@ui/collections/Form';
import Header from '@ui/elements/Header';
import { TextAreaProps } from '@ui/addons/TextArea';
import { Comment, Discussion } from '../../api/reviewer';
import { IsCommentUnpublished, DiscussionType } from './state';
import MarkdownGenerator from './markdownGenerator';

import './commentsView.less';
import { UserState } from '../../rootState';
import Radio from '@ui/addons/Radio';
import * as classNames from 'classnames';

export interface DiscussionActions {
    addNew(content: string, type: DiscussionType);
    addReply(parentId: string, content: string): void;
    editReply(commentId: string, content: string): void;
    removeUnpublishedComment(commentId: string): void;
    resolve(discussionId: string);
    unresolve(discussionId: string);
}

const DiscussionTypeSelector = (props: {
    type: DiscussionType;
    onChange: (type: DiscussionType) => void;
}): JSX.Element => {
    const names = {
        [DiscussionType.Comment]: 'Comment',
        [DiscussionType.NeedsResolution]: 'To fix',
        [DiscussionType.GoodWork]: 'Good work! Grab potato!',
    };

    const { type, onChange } = props;

    const Item = (props: { type: DiscussionType }) => (
        <Form.Field>
            <Radio label={names[props.type]} checked={type == props.type} onChange={() => onChange(props.type)} />
        </Form.Field>
    );
    return (
        <>
            <Item type={DiscussionType.Comment} />
            <Item type={DiscussionType.NeedsResolution} />
            <Item type={DiscussionType.GoodWork} />
        </>
    );
};

interface Reply {
    id: string;
    parentId: string;
    content: string;
}

interface CommentsState {
    commentText: string;
    discussionType: DiscussionType;
    newDiscussionVisible: boolean;
}

interface CommentProps {
    comment: Comment;
    actions: DiscussionActions;
    statusComponent?: JSX.Element;
    note?: JSX.Element;
    readOnly?: boolean;
}

interface CommentState {
    replyText: string;
    replyVisible: boolean;
    editText: string;
    editVisible: boolean;
}

const mapComments = (comments: Comment[], defaultProps: CommentProps): JSX.Element[] => {
    return comments.map((comment) => (
        <CommentComponent
            key={comment.id}
            comment={comment}
            actions={defaultProps.actions}
            readOnly={defaultProps.readOnly}
        />
    ));
};

class CommentComponent extends React.Component<CommentProps, CommentState> {
    constructor(props: CommentProps) {
        super(props);

        this.state = {
            replyText: '',
            replyVisible: false,
            editText: props.comment.content,
            editVisible: false,
        };
    }

    private getReplyTextAreaId = (): string => `text-reply-${this.props.comment.id}`;
    private getEditTextAreaId = (): string => `text-edit-${this.props.comment.id}`;

    render(): JSX.Element {
        const children = mapComments(this.props.comment.children, this.props);

        const switchReply = () => {
            this.setState({ replyVisible: !this.state.replyVisible, editVisible: false });

            if (!this.state.replyVisible) {
                setTimeout(() => {
                    const textArea = document.getElementById(this.getReplyTextAreaId());
                    textArea.focus();
                }, 0);
            }
        };

        const switchEdit = () => {
            this.setState({ editVisible: !this.state.editVisible, replyVisible: false });

            if (!this.state.editVisible) {
                setTimeout(() => {
                    const textArea = document.getElementById(this.getEditTextAreaId());
                    textArea.focus();
                }, 0);
            }
        };

        const onSubmit = () => {
            this.setState({ replyText: '', replyVisible: false });
        };

        const onSubmitEdited = () => {
            this.setState({ editVisible: false });
        };

        const onChangeReply = (event: React.FormEvent<HTMLTextAreaElement>, data: TextAreaProps) => {
            this.setState({ replyText: data.value.toString() });
        };

        const onChangeEdit = (event: React.FormEvent<HTMLTextAreaElement>, data: TextAreaProps) => {
            this.setState({ editText: data.value.toString() });
        };

        const form = (
            <Form reply onSubmit={onSubmit}>
                <Form.TextArea
                    id={this.getReplyTextAreaId()}
                    onChange={onChangeReply}
                    value={this.state.replyText}
                    placeholder="Reply..."
                />
                <Button
                    onClick={() => this.props.actions.addReply(this.props.comment.id, this.state.replyText)}
                    primary
                >
                    Add Comment
                </Button>
            </Form>
        );

        const editForm = (
            <Form reply onSubmit={onSubmitEdited}>
                <Form.TextArea id={this.getEditTextAreaId()} onChange={onChangeEdit} value={this.state.editText} />
                <Button
                    onClick={() => this.props.actions.editReply(this.props.comment.id, this.state.editText)}
                    primary
                >
                    Update response
                </Button>
            </Form>
        );

        const markdown = new MarkdownGenerator();

        const isUnpublished = IsCommentUnpublished(this.props.comment.id);

        const ack = '\ud83d\udc4d';
        const acknowledgeVisible =
            !this.props.readOnly &&
            !isUnpublished &&
            this.props.comment.children.length == 0 &&
            this.props.comment.content !== ack;
        const acknowledgeButton = !acknowledgeVisible ? null : (
            <UIComment.Action onClick={() => this.props.actions.addReply(this.props.comment.id, ack)}>
                {ack}
            </UIComment.Action>
        );

        return (
            <UIComment>
                <UIComment.Avatar src={this.props.comment.author.avatarUrl} />
                <UIComment.Content>
                    <UIComment.Author>{this.props.comment.author.name}</UIComment.Author>
                    <UIComment.Metadata>
                        <div>
                            {this.props.comment.createdAt && new Date(this.props.comment.createdAt).toLocaleString()}
                        </div>
                        {this.props.note}
                        {isUnpublished && 'Unpublished'}
                    </UIComment.Metadata>
                    <UIComment.Text>
                        {!this.state.editVisible && (
                            <div
                                dangerouslySetInnerHTML={{ __html: markdown.makeHtml(this.props.comment.content) }}
                            ></div>
                        )}
                        {this.state.editVisible && editForm}
                    </UIComment.Text>
                    <UIComment.Actions>
                        {isUnpublished && <UIComment.Action onClick={switchEdit}>Edit</UIComment.Action>}
                        {isUnpublished && (
                            <UIComment.Action
                                onClick={() => this.props.actions.removeUnpublishedComment(this.props.comment.id)}
                            >
                                Remove
                            </UIComment.Action>
                        )}
                        {!this.props.readOnly && !isUnpublished && (
                            <UIComment.Action active={this.state.replyVisible} onClick={switchReply}>
                                Reply
                            </UIComment.Action>
                        )}
                        {acknowledgeButton}
                        {!isUnpublished && this.props.statusComponent}
                    </UIComment.Actions>
                    {this.state.replyVisible ? form : null}
                    {children}
                </UIComment.Content>
            </UIComment>
        );
    }
}

interface DiscussionComponentProps {
    discussion: Discussion;
    actions: DiscussionActions;
    note?(discussion: Discussion): JSX.Element;
    unpublishedReplies: Reply[];
}
const DiscussionComponent = (props: DiscussionComponentProps) => {
    let status: JSX.Element = null;
    switch (props.discussion.state) {
        case 'NoActionNeeded':
            status = null;
            break;
        case 'NeedsResolution':
            if (props.discussion.canResolve) {
                const resolve = () => props.actions.resolve(props.discussion.id);
                status = <UIComment.Action onClick={resolve}>Resolve</UIComment.Action>;
            }
            break;
        case 'ResolvePending':
            const unresolve = () => props.actions.unresolve(props.discussion.id);
            status = (
                <UIComment.Action className="resolved-pending" onClick={unresolve}>
                    Resolved (pending)
                </UIComment.Action>
            );
            break;
        case 'Resolved':
            status = <span>Resolved</span>;
            break;
    }

    return (
        <div
            className={classNames({
                'read-only': props.discussion.state === 'Resolved',
                'good-work': props.discussion.state === 'GoodWork',
            })}
        >
            <CommentComponent
                comment={props.discussion.comment}
                statusComponent={status}
                actions={props.actions}
                note={props.note ? props.note(props.discussion) : null}
                readOnly={props.discussion.state === 'Resolved' || props.discussion.state === 'GoodWork'}
            />
        </div>
    );
};

const mergeCommentsWithReplies = (comments: Comment[], replies: Reply[], currentUser: UserState): Comment[] => {
    const result: Comment[] = [];

    const replyToComment = (reply: Reply): Comment => ({
        id: reply.id,
        author: currentUser,
        content: reply.content,
        createdAt: '',
        children: [] as Comment[],
    });

    for (const item of comments) {
        result.push({
            ...item,
            children: mergeCommentsWithReplies(
                [...item.children, ...replies.filter((r) => r.parentId == item.id).map(replyToComment)],
                replies,
                currentUser,
            ),
        });
    }

    return result;
};

interface DiscussionsProps {
    discussions: Discussion[];
    unpublishedReplies: Reply[];
    actions: DiscussionActions;
    currentUser: UserState;
    title?: string;
    replyOnly?: boolean;
    note?(discussion: Discussion): JSX.Element;
    discussionId: string;
}

export const getNewDiscussionTextAreaId = (discussionId: string): string => `text-comment-${discussionId}`;

export default class CommentsComponent extends React.Component<DiscussionsProps, CommentsState> {
    constructor(props: DiscussionsProps) {
        super(props);

        this.state = {
            commentText: '',
            discussionType: DiscussionType.NeedsResolution,
            newDiscussionVisible: false,
        };
    }

    render(): JSX.Element {
        const discussionsWithReplies = this.props.discussions.map((d) => ({
            ...d,
            comment: mergeCommentsWithReplies([d.comment], this.props.unpublishedReplies, this.props.currentUser)[0],
        }));

        const discussions = discussionsWithReplies.map((d) => (
            <DiscussionComponent
                key={d.id}
                discussion={d}
                actions={this.props.actions}
                note={this.props.note}
                unpublishedReplies={this.props.unpublishedReplies}
            />
        ));

        const onSubmit = () => {
            this.setState({ commentText: '' });
        };

        const onChangeReply = (event: React.FormEvent<HTMLTextAreaElement>, data: TextAreaProps) => {
            this.setState({ commentText: data.value.toString() });
        };

        const onChangeType = (t: DiscussionType) => {
            this.setState({ discussionType: t });
        };

        const addComment = () => {
            this.props.actions.addNew(this.state.commentText, this.state.discussionType);
            this.setState({ newDiscussionVisible: false });
        };

        const newDiscussion = (
            <Form reply onSubmit={onSubmit}>
                <Form.TextArea
                    id={getNewDiscussionTextAreaId(this.props.discussionId)}
                    onChange={onChangeReply}
                    value={this.state.commentText}
                    placeholder="Start new discussion..."
                />
                <Form.Group inline>
                    <Button onClick={addComment} secondary>
                        Add Comment
                    </Button>
                    {discussions.length > 0 ? (
                        <Button onClick={() => this.setState({ newDiscussionVisible: false })} secondary>
                            Cancel
                        </Button>
                    ) : null}
                    <DiscussionTypeSelector type={this.state.discussionType} onChange={onChangeType} />
                </Form.Group>
            </Form>
        );

        const showDiscussion = (
            <Button
                className={'start-another-discussion'}
                onClick={() => this.setState({ newDiscussionVisible: true })}
                basic
            >
                Start Another Discussion
            </Button>
        );

        const selectDiscussionView = () => {
            if (this.props.replyOnly) {
                return null;
            }

            if (discussions.length == 0 || this.state.newDiscussionVisible) {
                return newDiscussion;
            }

            return showDiscussion;
        };

        return (
            <UIComment.Group>
                <Header as="h3" dividing>
                    {this.props.title || 'Comments'}
                </Header>
                {discussions}
                {selectDiscussionView()}
            </UIComment.Group>
        );
    }
}
