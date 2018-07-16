import * as React from 'react';

import DiffView, { Props as DiffViewProps, LineWidget, DiffSide } from './diffView';
import { FileDiscussion, RevisionId } from '../../api/reviewer';
import CommentsView, { CommentsActions } from './commentsView';

export interface LineCommentsActions {
    showCommentsForLine(lineNumber: number): void;
    hideCommentsForLine(lineNumber: number): void;
    startFileDiscussion(lineNumber: number, content: string, needResolution: boolean): void;
}

interface CommentProps {
    comments: FileDiscussion[];
    commentActions: CommentsActions;
    lineCommentsActions: LineCommentsActions;
    visibleCommentLines: number[];
    leftSideRevision: RevisionId;
    rightSideRevision: RevisionId;
    pendingResolved: string[];
}

interface CalculatedProps {
    lineWidgets: any[];
    onLineClick: (side: DiffSide, line: number) => void;
}

type CalculatedFields = Exclude<keyof DiffViewProps, keyof CalculatedProps>;
type PassthroughProps = { [K in CalculatedFields]: DiffViewProps[K] };

type Props = PassthroughProps & CommentProps;

const buildLineWidgets = (props: Props) => {
    const lineComments = {
        left: new Map<number, Comment[]>(),
        right: new Map<number, Comment[]>()
    }
    
    for (let fileComment of props.comments) {
        let side = fileComment.revision == props.leftSideRevision ? 'left' : 'right';
        lineComments[side].set(fileComment.lineNumber, [
            ...(lineComments[side].get(fileComment.lineNumber) || []),
            {
                ...fileComment.comment,
                state: props.pendingResolved.indexOf(fileComment.comment.id) >= 0 ? 'ResolvePending' : fileComment.comment.state
            }
        ]);
    }
    
    for (let lineNumber of props.visibleCommentLines) {
        lineComments.right.set(lineNumber, lineComments.right.get(lineNumber) || []);
    }

    const lineWidgets: LineWidget[] = [];
    for(let side of ['left', 'right']) {
        for (let [lineNumber, comments] of lineComments[side]) {
            const commentActions: CommentsActions = {
                addNew: (content, needResolution) => {
                    props.lineCommentsActions.startFileDiscussion(lineNumber, content, needResolution);
                },
                addReply: (parentId, content) => {
                    
                },
                resolve: props.commentActions.resolve,
                unresolve: props.commentActions.unresolve
            }

            lineWidgets.push({
                lineNumber,
                side: side as DiffSide,
                widget: (
                    <CommentsView
                        comments={comments}
                        actions={commentActions}
                    />
                )
            })
        }
    }

    return lineWidgets;
}

export default (props: Props) => {
    const { 
        comments, 
        commentActions,
        leftSideRevision, 
        rightSideRevision,
        pendingResolved,
        ...diffViewProps 
    } = props;

    const lineWidgets = buildLineWidgets(props);

    const toggleChangeComment = (side, lineNumber) => {
        if (props.visibleCommentLines.indexOf(lineNumber) == -1) {
            props.lineCommentsActions.showCommentsForLine(lineNumber);
        } else {
            props.lineCommentsActions.hideCommentsForLine(lineNumber);
        }
    };

    return <DiffView 
        {...diffViewProps} 
        lineWidgets={lineWidgets}
        onLineClick={toggleChangeComment}
    />
};