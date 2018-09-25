import * as React from 'react';

import DiffView, { Props as DiffViewProps, LineWidget, DiffSide } from './diffView';
import { FileDiscussion, RevisionId, CommentReply, Discussion } from '../../api/reviewer';
import CommentsView, { DiscussionActions } from './commentsView';
import { UserState } from '../../rootState';
const style = require('./commentedDiffView.less');

export interface LineCommentsActions {
    showCommentsForLine(lineNumber: number): void;
    hideCommentsForLine(lineNumber: number): void;
    startFileDiscussion(lineNumber: number, content: string, needResolution: boolean): void;
}

interface CommentProps {
    comments: FileDiscussion[];
    commentActions: DiscussionActions;
    lineCommentsActions: LineCommentsActions;
    visibleCommentLines: number[];
    leftSideRevision: RevisionId;
    rightSideRevision: RevisionId;
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    currentUser: UserState;
}

interface CalculatedProps {
    lineWidgets: any[];
    onLineClick: (side: DiffSide, line: number) => void;
}

type CalculatedFields = Exclude<keyof DiffViewProps, keyof CalculatedProps>;
type PassthroughProps = { [K in CalculatedFields]: DiffViewProps[K] };

type Props = PassthroughProps & CommentProps;

interface LineComments {
    left: Map<number, FileDiscussion[]>;
    right: Map<number, FileDiscussion[]>;
    unmatched: FileDiscussion[];
}

const splitComments = (props: Props): LineComments => {
    const lineComments: LineComments = {
        left: new Map<number, FileDiscussion[]>(),
        right: new Map<number, FileDiscussion[]>(),
        unmatched: []
    }

    for (let fileComment of props.comments) {

        if (fileComment.revision == props.rightSideRevision || fileComment.revision == props.leftSideRevision) {
            let side = fileComment.revision == props.rightSideRevision ? 'right' : 'left';
            lineComments[side].set(fileComment.lineNumber, [
                ...(lineComments[side].get(fileComment.lineNumber) || []),
                {
                    ...fileComment,
                    state: props.pendingResolved.indexOf(fileComment.id) >= 0 ? 'ResolvePending' : fileComment.state
                }
            ]);
        } else {
            lineComments.unmatched.push({
                ...fileComment,
                state: props.pendingResolved.indexOf(fileComment.id) >= 0 ? 'ResolvePending' : fileComment.state
            });
        }
    }

    for (let lineNumber of props.visibleCommentLines) {
        lineComments.right.set(lineNumber, lineComments.right.get(lineNumber) || []);
    }

    return lineComments;
}

const buildCommentView = (props: Props, lineNumber: number, discussions: Discussion[]) => {
    const commentActions: DiscussionActions = {
        addNew: (content, needResolution) => {
            props.lineCommentsActions.startFileDiscussion(lineNumber, content, needResolution);
        },
        addReply: (parentId, content) => {
            props.commentActions.addReply(parentId, content)
        },
        resolve: props.commentActions.resolve,
        unresolve: props.commentActions.unresolve
    }

    return (
        <CommentsView
            discussions={discussions}
            actions={commentActions}
            unpublishedReplies={props.unpublishedReplies}
            currentUser={props.currentUser}
        />
    )
};

const buildLineWidgets = (props: Props, lineComments: LineComments) => {
    const lineWidgets: LineWidget[] = [];
    for (let side of ['left', 'right']) {
        for (let [lineNumber, discussions] of lineComments[side]) {
            lineWidgets.push({
                lineNumber,
                side: side as DiffSide,
                widget: buildCommentView(props, lineNumber, discussions)
            });
        }
    }

    return lineWidgets;
}

const UnmatchedComments = (props: {
    unpublishedReplies: CommentReply[]; 
    commentActions: DiscussionActions;
    currentUser: UserState;
    discussions: FileDiscussion[];
}) => {
    const commentActions: DiscussionActions = {
        addNew: (content, needResolution) => {
            throw 'Not supported';
        },
        addReply: (parentId, content) => {
            props.commentActions.addReply(parentId, content)
        },
        resolve: props.commentActions.resolve,
        unresolve: props.commentActions.unresolve
    }

    const note = (d: Discussion): JSX.Element => <div className="unmatched-comment-info">Revision {d.revision} line {(d as FileDiscussion).lineNumber}</div>;

    return (
        <CommentsView
            title="Unmatched comments"
            replyOnly={true}
            note={note}
            discussions={props.discussions}
            actions={commentActions}
            unpublishedReplies={props.unpublishedReplies}
            currentUser={props.currentUser}
        />
    );
}

export default (props: Props) => {
    const {
        comments,
        commentActions,
        leftSideRevision,
        rightSideRevision,
        pendingResolved,
        unpublishedReplies,
        ...diffViewProps
    } = props;

    const lineComments = splitComments(props);

    const lineWidgets = buildLineWidgets(props, lineComments);

    const toggleChangeComment = (side, lineNumber) => {
        if (props.visibleCommentLines.indexOf(lineNumber) == -1) {
            props.lineCommentsActions.showCommentsForLine(lineNumber);
        } else {
            props.lineCommentsActions.hideCommentsForLine(lineNumber);
        }
    };

    return (
    <div>
        <DiffView
            {...diffViewProps}
            lineWidgets={lineWidgets}
            onLineClick={toggleChangeComment}
        />
        {lineComments.unmatched.length > 0 ? 
            <UnmatchedComments unpublishedReplies={props.unpublishedReplies} currentUser={props.currentUser} discussions={lineComments.unmatched} commentActions={props.commentActions}/> 
        : null}
    </div>
    );
};