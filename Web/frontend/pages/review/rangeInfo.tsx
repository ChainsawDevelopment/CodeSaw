import * as React from "react";

import Menu from '@ui/collections/Menu';
import Button from '@ui/elements/Button';
import Segment from '@ui/elements/Segment';
import Sticky from '@ui/modules/Sticky';
import Icon from '@ui/elements/Icon';
import Popup from '@ui/modules/Popup';
import scrollToComponent from 'react-scroll-to-component';
import { FileLink } from "./FileLink";

import * as PathPairs from "../../pathPair";
import { ReviewId, FileDiscussion, CommentReply, FileToReview } from "../../api/reviewer";
import { FileInfo } from "./state";

import CommentedDiffView, { LineCommentsActions } from './commentedDiffView';
import { DiscussionActions } from "./commentsView";
import FileSummary from './fileSummary';
import ChangedFileTreePopup from "./fileTreePopup";
import ReviewMark from "./reviewMark";
import { UserState } from "../../rootState";
import { DiffType } from "./diffView";
import { HotKeys } from "../../components/HotKeys";

interface FileViewProps {
    file: FileInfo;
    comments: FileDiscussion[];
    unpublishedFileDiscussions: FileDiscussion[];
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    commentActions: DiscussionActions;
    currentUser: UserState;
    startFileDiscussion(path: PathPairs.PathPair, lineNumber: number, content: string, needsResolution: boolean): void;
}

class FileView extends React.Component<FileViewProps, { visibleCommentLines: number[] }> {
    private renderedRef: HTMLSpanElement;

    constructor(props: FileViewProps) {
        super(props);

        this.state = {
            visibleCommentLines: []
        };
    }

    public componentDidUpdate(prevProps: FileViewProps, prevState: { visibleCommentLines: number[] }) {
        if (!PathPairs.equal(prevProps.file.path, this.props.file.path)) {
            this.setState({visibleCommentLines: []});
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
    }

    render(): JSX.Element {
        const { file, commentActions } = this.props;

        const fileDiscussions = this.props.comments
            .filter(f =>
                PathPairs.equal(f.filePath, file.path)
                && (f.revision == file.fileToReview.current || f.revision == file.fileToReview.previous || (f.revision as number) + 1 == file.fileToReview.previous))
            ;

        const unpublishedDiscussion = this.props.unpublishedFileDiscussions
            .filter(f => PathPairs.equal(f.filePath, file.path));

        const lineCommentsActions: LineCommentsActions = {
            hideCommentsForLine: l => this.hideLine(l),
            showCommentsForLine: l => this.showLine(l),
            startFileDiscussion: (lineNumber, content, needResolution) => {
                this.props.startFileDiscussion(file.path, lineNumber, content, needResolution)
            }
        }

        const diffTypes = {
            'modified': 'modify' as DiffType,
            'created': 'add' as DiffType,
            'deleted': 'delete' as DiffType,
            'renamed': 'modify' as DiffType
        };

        return (
            <span ref={span => this.renderedRef = span}>
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
                    /> 
                    : null}
            </span>
        );
    }
}

const NoFileView = () => {
    return (
        <span className="no-file-selected">Select file to see diff</span>
    )
};

export type SelectFileForViewHandler = (path: PathPairs.PathPair) => void;
export type OnShowFileHandlerAvailable = (handler: () => void) => void;

export interface ReviewFileActions {
    review(file: PathPairs.PathPair): void;
    unreview(file: PathPairs.PathPair): void;
}

export interface Props {
    filesToReview: FileToReview[];
    selectedFile: FileInfo & { isReviewed: boolean };
    onSelectFileForView: SelectFileForViewHandler;
    reviewFile: ReviewFileActions;
    reviewedFiles: PathPairs.List;
    publishReview(): void;
    onShowFileHandlerAvailable: OnShowFileHandlerAvailable;
    reviewId: ReviewId;
    fileComments: FileDiscussion[];
    unpublishedFileDiscussion: FileDiscussion[];
    startFileDiscussion(path: PathPairs.PathPair, lineNumber: number, content: string, needsResolution: boolean): void;
    commentActions: DiscussionActions;
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    currentUser: UserState;
}

export default class RangeInfo extends React.Component<Props, { stickyContainer: HTMLDivElement }> {
    constructor(props: Props) {
        super(props);
        this.state = { stickyContainer: null };
    }

    private _handleRef = el => {
        this.setState({ stickyContainer: el });
        this.props.onShowFileHandlerAvailable(() => scrollToComponent(el, { offset: 0, align: 'top', duration: 100, ease: 'linear' }));
    }

    private _changeFileReviewState = (newState: boolean) => {
        if (newState) {
            this.props.reviewFile.review(this.props.selectedFile.path);
        } else {
            this.props.reviewFile.unreview(this.props.selectedFile.path);
        }
    }

    private _findNextUnreviewedFile = (current: PathPairs.PathPair, direction: 1 | -1): PathPairs.PathPair => {
        const changes = this.props.filesToReview.filter(f => f.current != f.previous);
        const currentIndex = changes.findIndex(p => PathPairs.equal(p.reviewFile, this.props.selectedFile.path));

        if (currentIndex == -1) {
            return null;
        }

        const filesReviewedByUser = this.props.reviewedFiles;

        let index = currentIndex;

        while (true) {
            index += direction;

            if (index == -1) {
                index = changes.length - 1;
            } else if (index == changes.length) {
                index = 0;
            }

            if (index == currentIndex) {
                return current;
            }

            const candidate = changes[index].reviewFile;

            if (!PathPairs.contains(filesReviewedByUser, candidate)) {
                return candidate;
            }
        }
    }

    render() {
        const { selectedFile, onSelectFileForView } = this.props;

        const menuItems = [];
        let reviewHotKeys = {}

        if (selectedFile) {
            const nextFile = this._findNextUnreviewedFile(selectedFile.path, 1);
            const prevFile = this._findNextUnreviewedFile(selectedFile.path, -1);

            reviewHotKeys = {
                '[': () => prevFile && onSelectFileForView(prevFile),
                ']': () => nextFile && onSelectFileForView(nextFile),
                'y': () => this._changeFileReviewState(!this.props.selectedFile.isReviewed),
                'ctrl+Enter': this.props.publishReview
            };
            
            menuItems.push(<Menu.Item fitted key="review-mark">
                <Popup
                    trigger={<ReviewMark reviewed={this.props.selectedFile.isReviewed} onClick={this._changeFileReviewState} />}
                    content="Toggle review status"
                />

            </Menu.Item>);
            menuItems.push(<Menu.Item fitted key="refresh-diff">
                <Popup
                    trigger={<Icon onClick={() => onSelectFileForView(selectedFile.path)} name="redo" circular link color="blue"></Icon>}
                    content="Refresh file diff"
                />
            </Menu.Item>);
            menuItems.push(<Menu.Item fitted key="file-navigation">
                {prevFile &&
                    <Popup
                        trigger={<FileLink reviewId={this.props.reviewId} path={prevFile} >
                            <Icon name="step backward" circular link /></FileLink>}
                        content="Previous unreviewed file"
                    />}
                {nextFile &&
                    <Popup

                        trigger={<FileLink reviewId={this.props.reviewId} path={nextFile} >
                            <Icon name="step forward" circular link /></FileLink>}
                        content="Next unreviewed file"
                    />}
            </Menu.Item>);
            menuItems.push(<Menu.Item fitted key="file-path">
                <span className="file-path">{selectedFile.path.newPath}</span>
            </Menu.Item>);
        }

        const selectableFiles = this.props.filesToReview.map(i => i.reviewFile);

        return (
            <div ref={this._handleRef}>
                <HotKeys config={reviewHotKeys} />
                <Segment>
                    <Sticky context={this.state.stickyContainer} id="file-sticky">
                        <Menu secondary id="file-menu">
                            {menuItems}
                            <Menu.Menu position='right'>
                                <Menu.Item>
                                    <Button positive onClick={this.props.publishReview}>Publish Changes</Button>
                                    &nbsp;
                                    <ChangedFileTreePopup
                                        paths={selectableFiles}
                                        selected={selectedFile ? selectedFile.path : PathPairs.emptyPathPair}
                                        reviewedFiles={this.props.reviewedFiles}
                                        onSelect={onSelectFileForView}
                                        reviewId={this.props.reviewId}
                                    />
                                </Menu.Item>
                            </Menu.Menu>
                        </Menu>
                    </Sticky>
                    <div>
                        {selectedFile ?
                            <FileView
                                file={selectedFile}
                                commentActions={this.props.commentActions}
                                comments={this.props.fileComments}
                                startFileDiscussion={this.props.startFileDiscussion}
                                unpublishedFileDiscussions={this.props.unpublishedFileDiscussion}
                                pendingResolved={this.props.pendingResolved}
                                unpublishedReplies={this.props.unpublishedReplies}
                                currentUser={this.props.currentUser}
                            />
                            : <NoFileView />
                        }
                    </div>
                </Segment>
            </div>
        );
    }
}