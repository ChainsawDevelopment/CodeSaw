import * as React from "react";
import { Button, Comment as UIComment, Form, Header, TextAreaProps } from "semantic-ui-react"
import { ReviewId, Comment } from "../../api/reviewer";
import { selectCurrentRevisions, selectFileForView, loadReviewInfo, rememberRevision, FileInfo } from "./state";

interface CommentsProps {
    comments: Comment[];
    reviewId: ReviewId;
    addComment(reviewId: ReviewId, content: string, parentId?: string);
}

interface CommentsState {
    commentText: string;
}

interface CommentProps extends Comment {
    reviewId: ReviewId;
    addComment(reviewId: ReviewId, content: string, parentId?: string);
}

interface CommentState {
    replyText: string;
    replyVisible: boolean;
}

const mapComments = (
    comments: Comment[],
    reviewId: ReviewId,
    addComment: (reviewId: ReviewId, content: string, parentId?: string) => void
): JSX.Element[] => {
    return comments.map(comment => (
        <CommentComponent
            key={comment.id}
            id={comment.id}
            author={comment.author}
            content={comment.content}
            createdAt={comment.createdAt}
            children={comment.children}
            reviewId={reviewId}
            addComment={addComment}
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
        const children = mapComments(this.props.children, this.props.reviewId, this.props.addComment);

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
                <Button onClick={() => this.props.addComment(this.props.reviewId, this.state.replyText, this.props.id)} primary>Add Comment</Button>
            </Form>
        );

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
                        <UIComment.Action active={this.state.replyVisible}><span onClick={switchReply}>Reply</span></UIComment.Action>
                        {this.state.replyVisible ? form : null}
                    </UIComment.Actions>
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
            commentText: ''
        };
    }

    render(): JSX.Element {
        const comments = mapComments(this.props.comments, this.props.reviewId, this.props.addComment);

        const onSubmit = () => {
            this.setState({ commentText: '' });
        };

        const onChangeReply = (event: React.FormEvent<HTMLTextAreaElement>, data: TextAreaProps) => {
            this.setState({ commentText: data.value.toString() });
        };

        return (
            <UIComment.Group>
                <Header as='h3' dividing>
                    Comments
                </Header>
                {comments}
                <Form reply onSubmit={onSubmit}>
                    <Form.TextArea onChange={onChangeReply} value={this.state.commentText} />
                    <Button onClick={() => this.props.addComment(this.props.reviewId, this.state.commentText)} primary>Add Comment</Button>
                </Form>
            </UIComment.Group>
        );
    }
}
