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
import { ReviewId, FileDiscussion, CommentReply, FileToReview, FileId } from "../../api/reviewer";
import { FileInfo } from "./state";

import CommentedDiffView, { LineCommentsActions } from './commentedDiffView';
import { DiscussionActions, getNewDiscussionTextAreaId } from "./commentsView";
import FileSummary from './fileSummary';
import ChangedFileTreePopup from "./fileTreePopup";
import ReviewMark from "./reviewMark";
import { UserState } from "../../rootState";
import { DiffType } from "./diffView";
import { HotKeys } from "../../components/HotKeys";
import { PublishButton } from "./PublishButton";
import ReviewMode from "./reviewMode";

interface FileViewProps {
    file: FileInfo;
    comments: FileDiscussion[];
    unpublishedFileDiscussions: FileDiscussion[];
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    commentActions: DiscussionActions;
    currentUser: UserState;
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean): void;
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
            this.setState({ visibleCommentLines: [] });
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
            .filter(f => f.fileId  == file.fileId);

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

export type SelectFileForViewHandler = (fileId: FileId) => void;
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
    reviewedFiles: FileId[];
    publishReview(): void;
    onShowFileHandlerAvailable: OnShowFileHandlerAvailable;
    reviewId: ReviewId;
    fileComments: FileDiscussion[];
    unpublishedFileDiscussion: FileDiscussion[];
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean): void;
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

    private _findNextFile = (current: FileId, direction: 1 | -1, predicate: (FileToReview) => boolean): FileId => {
        const changes = this.props.filesToReview;
        const currentIndex = changes.findIndex(p => PathPairs.equal(p.reviewFile, this.props.selectedFile.path));

        if (currentIndex == -1) {
            return null;
        }

        const filesReviewedByUser: FileId[] = this.props.reviewedFiles;

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

            const candidate = changes[index].fileId;

            if (predicate(changes[index])) {
                return candidate;
            }
        }
    }

    private _findNextUnreviewedFile = (current: FileId, direction: 1 | -1): FileId => {
        const predicate = (file: FileToReview) => {
            const filesReviewedByUser: FileId[] = this.props.reviewedFiles;

            const fileDiscussions = this.props.fileComments
                .filter(f => f.fileId == file.fileId)
                .filter(f => f.state === "NeedsResolution");

            if (fileDiscussions.length > 0) {
                return true;
            }

            if (filesReviewedByUser.indexOf(file.fileId) == -1) {
                return true;
            }

            return false;
        }
        
        return this._findNextFile(current, direction, predicate);
    }

    private _findNextFileWithUnresolvedComment = (current: FileId, direction: 1 | -1): FileId => {
        const predicate = (file: FileToReview) => {
            const fileDiscussions = this.props.fileComments
                .filter(f => f.fileId == file.fileId)
                .filter(f => f.state === "NeedsResolution" 
                        && f.comment.children.length == 0);

            if (fileDiscussions.length > 0) {
                return true;
            }

            return false;
        }

        return this._findNextFile(current, direction, predicate);
    }

    render() {
        const { selectedFile, onSelectFileForView } = this.props;

        const menuItems = [];
        let reviewHotKeys = {}

        if (selectedFile) {
            const nextFile = this._findNextUnreviewedFile(selectedFile.fileId, 1);
            const prevFile = this._findNextUnreviewedFile(selectedFile.fileId, -1);

            const nextFileWithUnresolvedComment = this._findNextFileWithUnresolvedComment(selectedFile.fileId, 1);
            const prevFileWithUnresolvedComment = this._findNextFileWithUnresolvedComment(selectedFile.fileId, -1);

            reviewHotKeys = {
                '[': () => prevFile && onSelectFileForView(prevFile),
                ']': () => nextFile && onSelectFileForView(nextFile),
                '{': () => prevFileWithUnresolvedComment && onSelectFileForView(prevFileWithUnresolvedComment),
                '}': () => nextFileWithUnresolvedComment && onSelectFileForView(nextFileWithUnresolvedComment),
                'y': () => this._changeFileReviewState(!this.props.selectedFile.isReviewed),
                'ctrl+Enter': this.props.publishReview
            };

            menuItems.push(
                <ReviewMode key="review-mark">
                    <ReviewMode.Reviewer>
                        <Menu.Item fitted>
                            <Popup
                                trigger={<ReviewMark reviewed={this.props.selectedFile.isReviewed} onClick={this._changeFileReviewState} />}
                                content="Toggle review status"
                            />
                        </Menu.Item>
                    </ReviewMode.Reviewer>
                    <ReviewMode.Author>
                        <Menu.Item fitted>
                            <Icon circular inverted name='eye' color='grey' />
                        </Menu.Item>
                    </ReviewMode.Author>
                </ReviewMode>);
            menuItems.push(<Menu.Item fitted key="refresh-diff">
                <Popup
                    trigger={<Icon onClick={() => onSelectFileForView(selectedFile.fileId)} name="redo" circular link color="blue"></Icon>}
                    content="Refresh file diff"
                />
            </Menu.Item>);
            menuItems.push(<Menu.Item fitted key="file-navigation">
                {prevFile &&
                    <Popup
                        trigger={<FileLink reviewId={this.props.reviewId} fileId={prevFile} >
                            <Icon name="step backward" circular link /></FileLink>}
                        content="Previous unreviewed file"
                    />}
                {nextFile &&
                    <Popup

                        trigger={<FileLink reviewId={this.props.reviewId} fileId={nextFile} >
                            <Icon name="step forward" circular link /></FileLink>}
                        content="Next unreviewed file"
                    />}
            </Menu.Item>);
            menuItems.push(<Menu.Item fitted key="file-path">
                <span className="file-path">{selectedFile.path.newPath}</span>
            </Menu.Item>);
        } else if (this.props.filesToReview.length > 0) {
            const firstFile = this.props.filesToReview[0].fileId;
            const lastFile = this.props.filesToReview[this.props.filesToReview.length - 1].fileId;

            reviewHotKeys = {
                '[': () => lastFile && onSelectFileForView(lastFile),
                ']': () => firstFile && onSelectFileForView(firstFile)
            };
        }

        const selectableFiles = this.props.filesToReview.map(i => ({ id: i.fileId, name: i.reviewFile }));

        return (
            <div ref={this._handleRef}>
                <HotKeys config={reviewHotKeys} />
                <Segment>
                    <Sticky context={this.state.stickyContainer} id="file-sticky">
                        <Menu secondary id="file-menu">
                            {menuItems}
                            <Menu.Menu position='right'>
                                <Menu.Item>
                                    <PublishButton />
                                    &nbsp;
                                    <ChangedFileTreePopup
                                        files={selectableFiles}
                                        selected={selectedFile ? selectedFile.fileId : null}
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
                                comments={this.props.selectedFile.discussions}
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