import * as React from "react";

import Segment from '@ui/elements/Segment';
import Sticky from '@ui/modules/Sticky';
import scrollToComponent from 'react-scroll-to-component';

import * as PathPairs from "../../pathPair";
import { ReviewId, FileDiscussion, CommentReply, FileToReview, FileId } from "../../api/reviewer";
import { FileInfo } from "./state";

import { DiscussionActions } from "./commentsView";
import { UserState } from "../../rootState";
import { HotKeys } from "../../components/HotKeys";

import { FileView, NoFileView } from "./sections/fileView";
import FileList from '@src/fileList';
import DiffHeader from "./sections/DiffHeader";

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
    fileComments: FileDiscussion[];
    unpublishedFileDiscussion: FileDiscussion[];
    startFileDiscussion(fileId: FileId, lineNumber: number, content: string, needsResolution: boolean): void;
    commentActions: DiscussionActions;
    pendingResolved: string[];
    unpublishedReplies: CommentReply[];
    currentUser: UserState;
    markNonEmptyAsViewed: any;
}

export default class RangeInfo extends React.Component<Props, { stickyContainer: HTMLDivElement }> {
    constructor(props: Props) {
        super(props);
        this.state = { stickyContainer: null };
    }

    private scrollToFile = () => {
        scrollToComponent(this.state.stickyContainer, { offset: 0, align: 'top', duration: 100, ease: 'linear' })
    }

    private _handleRef = el => {
        this.setState({ stickyContainer: el });
        this.props.onShowFileHandlerAvailable(this.scrollToFile);
    }

    private _changeFileReviewState = (newState: boolean) => {
        if (newState) {
            this.props.reviewFile.review(this.props.selectedFile.path);
        } else {
            this.props.reviewFile.unreview(this.props.selectedFile.path);
        }
    }

    render() {
        const { selectedFile, onSelectFileForView } = this.props;

        let reviewHotKeys = {}

        if (selectedFile) {
            const fileList = new FileList(
                this.props.filesToReview,
                this.props.selectedFile.fileId,
                this.props.reviewedFiles,
                this.props.fileComments
            );
            const nextFile = fileList.nextUnreviewedFile(+1);
            const prevFile = fileList.nextUnreviewedFile(-1);

            const nextFileWithUnresolvedComment = fileList.nextFileWithUnresolvedComment(+1);
            const prevFileWithUnresolvedComment = fileList.nextFileWithUnresolvedComment(-1);

            reviewHotKeys = {
                '[': () => prevFile && onSelectFileForView(prevFile.fileId),
                ']': () => nextFile && onSelectFileForView(nextFile.fileId),
                '{': () => prevFileWithUnresolvedComment && onSelectFileForView(prevFileWithUnresolvedComment.fileId),
                '}': () => nextFileWithUnresolvedComment && onSelectFileForView(nextFileWithUnresolvedComment.fileId),
                'y': () => this._changeFileReviewState(!this.props.selectedFile.isReviewed),
                'ctrl+Enter': this.props.publishReview,
                'ctrl+y': () => this.props.markNonEmptyAsViewed()
            };
        } else if (this.props.filesToReview.length > 0) {
            const firstFile = this.props.filesToReview[0].fileId;
            const lastFile = this.props.filesToReview[this.props.filesToReview.length - 1].fileId;

            reviewHotKeys = {
                '[': () => lastFile && onSelectFileForView(lastFile),
                ']': () => firstFile && onSelectFileForView(firstFile),
                'ctrl+y': () => this.props.markNonEmptyAsViewed()
            };
        }

        return (
            <div ref={this._handleRef}>
                <HotKeys config={reviewHotKeys} />
                <Segment>
                    <Sticky context={this.state.stickyContainer} id="file-sticky">
                        <DiffHeader onSelectFileForView={this.props.onSelectFileForView} />
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
                                scrollToFile={this.scrollToFile}
                            />
                            : <NoFileView />
                        }
                    </div>
                </Segment>
            </div>
        );
    }
}