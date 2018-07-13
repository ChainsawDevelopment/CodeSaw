import * as React from "react";
import { Button, Checkbox, CheckboxProps, Comment as UIComment, Form, Header, TextAreaProps } from "semantic-ui-react"
import { ReviewId, Comment } from "../../api/reviewer";

export interface CommentsActions {
    add(content: string, needsResolution: boolean, parentId?: string);
    resolve(commentId: string);
}

interface CommentsProps {
    comments: Comment[];
    actions: CommentsActions;
}

interface CommentsState {
    commentText: string;
    needsResolution: boolean;
}

interface CommentProps extends Comment {
    actions: CommentsActions;
}

interface CommentState {
    replyText: string;
    replyVisible: boolean;
}

const mapComments = (
    comments: Comment[],
    actions: CommentsActions
): JSX.Element[] => {
    return comments.map(comment => (
        <CommentComponent
            key={comment.id}
            id={comment.id}
            author={comment.author}
            content={comment.content}
            state={comment.state}
            createdAt={comment.createdAt}
            children={comment.children}
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
        const children = mapComments(this.props.children, this.props.actions);

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
                <Button onClick={() => this.props.actions.add(this.state.replyText, false, this.props.id)} primary>Add Comment</Button>
            </Form>
        );

        const resolveComment = () => {
            this.props.actions.resolve(this.props.id);
        }

        const renderStatus = () => {
            switch (this.props.state) {
                case 'NoActionNeeded':
                    return;
                case 'NeedsResolution':
                    return <UIComment.Action onClick={resolveComment}>Resolve</UIComment.Action>
                case 'Resolved':
                    return <span>Resolved</span>;
            }
        };

        return (
            <UIComment>
                <UIComment.Avatar src='http://rs300.pbsrc.com/albums/nn5/jezzeble/cow%20and%20chicken/cow_chicken.gif~c200' />
                <UIComment.Content>
                    <UIComment.Author>{this.props.author}</UIComment.Author>
                    <UIComment.Metadata>
                        <div>{this.props.createdAt}</div>
                    </UIComment.Metadata>
                    <UIComment.Text>{this.props.content}</UIComment.Text>
                    <UIComment.Actions>
                        <UIComment.Action active={this.state.replyVisible} onClick={switchReply}>Reply</UIComment.Action>
                        {renderStatus()}
                    </UIComment.Actions>
                    {this.state.replyVisible ? form : null}
                    {children}
                </UIComment.Content>
            </UIComment>
        )
    }
}

export default class CommentsComponent extends React.Component<CommentsProps, CommentsState> {
    constructor(props: CommentsProps) {
        super(props);

        this.state = {
            commentText: '',
            needsResolution: false
        };
    }

    render(): JSX.Element {
        const comments = mapComments(this.props.comments, this.props.actions);

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
                {comments}
                <Form reply onSubmit={onSubmit}>
                    <Form.TextArea onChange={onChangeReply} value={this.state.commentText} />
                    <Button onClick={() => this.props.actions.add(this.state.commentText, this.state.needsResolution)} secondary>Add Comment</Button>
                    <Checkbox onChange={onChangeNeedsResolution} checked={this.state.needsResolution} label="Needs resolution" />
                </Form>
            </UIComment.Group>
        );
    }
}
