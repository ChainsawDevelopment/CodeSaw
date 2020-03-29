import { FileInfo } from "../state";
import { FileDiscussion, CommentReply, FileId } from "@api/reviewer";
import { DiscussionActions, getNewDiscussionTextAreaId } from "../commentsView";
import { UserState } from "@src/rootState";
import * as React from "react";
import * as PathPairs from '@src/pathPair';
import CommentedDiffView, { LineCommentsActions } from "../commentedDiffView";
import { DiffType } from "../diffView";
import FileSummary from '../fileSummary';

interface FileViewProps {
    file: FileInfo;
    comments: FileDiscussion[];
    unpublishedFileDiscussions: FileDiscussion[];
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    commentActions: DiscussionActions;
    currentUser: UserState;
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean): void;
    scrollToFile(): void;
}

export class FileView extends React.Component<FileViewProps, { visibleCommentLines: number[] }> {
    private renderedRef: HTMLSpanElement;

    constructor(props: FileViewProps) {
        super(props);

        this.state = {
            visibleCommentLines: []
        };
    }

    public componentDidUpdate(prevProps: FileViewProps, prevState: { visibleCommentLines: number[] }) {
        if (!PathPairs.equal(prevProps.file.path, this.props.file.path)) {
            this.setState({ visibleCommentLines: [] });
        }
    }

    public componentDidMount() {
        if (this.props.scrollToFile) {
            this.props.scrollToFile();
        }

    }

    private hideLine(line: number) {
        this.setState({
            visibleCommentLines: this.state.visibleCommentLines.filter(f => f != line)
        });
    }

    private showLine(line: number) {
        this.setState({
            visibleCommentLines: [...this.state.visibleCommentLines, line]
        });

        setTimeout(() => {
            document.getElementById(getNewDiscussionTextAreaId(line.toString())).focus();
        }, 0);
    }

    render(): JSX.Element {
        const { file, commentActions } = this.props;

        const fileDiscussions = this.props.comments
            .filter(f => f.fileId == file.fileId);

        const unpublishedDiscussion = this.props.unpublishedFileDiscussions
            .filter(f => f.fileId == file.fileId);

        const lineCommentsActions: LineCommentsActions = {
            hideCommentsForLine: l => this.hideLine(l),
            showCommentsForLine: l => this.showLine(l),
            startFileDiscussion: (lineNumber, content, needResolution) => {
                this.props.startFileDiscussion(file.fileId, lineNumber, content, needResolution)
            }
        }

        const diffTypes = {
            'modified': 'modify' as DiffType,
            'created': 'add' as DiffType,
            'deleted': 'delete' as DiffType,
            'renamed': 'modify' as DiffType
        };

        const language = file.path.newPath.split('.').slice(-1)[0];

        return (
            <span ref={span => this.renderedRef = span} key={file.fileId}>
                <FileSummary file={file} />
                {file.diff ?
                    <CommentedDiffView
                        diffInfo={file.diff}
                        comments={fileDiscussions.concat(unpublishedDiscussion)}
                        commentActions={commentActions}
                        leftSideRevision={file.fileToReview.previous}
                        rightSideRevision={file.fileToReview.current}
                        visibleCommentLines={this.state.visibleCommentLines}
                        lineCommentsActions={lineCommentsActions}
                        pendingResolved={this.props.pendingResolved}
                        unpublishedReplies={this.props.unpublishedReplies}
                        currentUser={this.props.currentUser}
                        contents={this.props.file.diff.contents.review}
                        type={diffTypes[this.props.file.fileToReview.changeType]}
                        language={language}
                    />
                    : null}
            </span>
        );
    }
}

export const NoFileView = () => {
    return (
        <span className="no-file-selected">Select file to see diff</span>
    )
};
