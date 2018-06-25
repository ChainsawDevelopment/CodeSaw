import * as React from "react";
import Menu from '@ui/collections/Menu';
import Button from '@ui/elements/Button';
import Segment from '@ui/elements/Segment';
import Sticky from '@ui/modules/Sticky';
import { RevisionRangeInfo } from "../../api/reviewer";
import DiffView from './diffView';
import FileSummary from './fileSummary';
import ChangedFileTreePopup from "./fileTreePopup";
import { FileInfo } from "./state";
import ReviewMark from "./reviewMark";
import { PathPair, emptyPathPair } from "../../pathPair";
import * as PathPairs from "../../pathPair";
import Icon from '@ui/elements/Icon';
import Popup from '@ui/modules/Popup';
import scrollToComponent from 'react-scroll-to-component';

interface FileViewProps {
    file: FileInfo
}

class FileView extends React.Component<FileViewProps> {
    private renderedRef: HTMLSpanElement;

    render(): JSX.Element {
        const { file } = this.props;

        return (
            <span ref={span => this.renderedRef = span}>
                <FileSummary file={file} />
                {file.diff ? <DiffView hunks={file.diff.hunks} /> : null}
            </span>
        );
    }

    componentDidUpdate() {
        scrollToComponent(this.renderedRef, {offset: -75, align: 'top', duration: 100, ease:'linear'});
    }
}

const NoFileView = () => {
    return (
        <span className="no-file-selected">Select file to see diff</span>
    )
};

export type SelectFileForViewHandler = (path: PathPair) => void;

export interface ReviewFileActions {
    review(file: PathPair): void;
    unreview(file: PathPair): void;
}

export interface Props {
    info: RevisionRangeInfo;
    selectedFile: FileInfo & { isReviewed: boolean };
    onSelectFileForView: SelectFileForViewHandler;
    reviewFile: ReviewFileActions;
    reviewedFiles: PathPair[];
    publishReview(): void;
}

export default class RangeInfo extends React.Component<Props, { stickyContainer: HTMLDivElement }> {
    constructor(props: Props) {
        super(props);
        this.state = { stickyContainer: null };
    }

    private _handleRef = el => {
        this.setState({ stickyContainer: el });
    }

    private _changeFileReviewState = (newState: boolean) => {
        if (newState) {
            this.props.reviewFile.review(this.props.selectedFile.path);
        } else {
            this.props.reviewFile.unreview(this.props.selectedFile.path);
        }
    }

    private _findNextUnreviewedFile = (current: PathPair, direction: 1 | -1): PathPair => {
        const currentIndex = this.props.info.changes.findIndex(p => PathPairs.equal(p.path, this.props.selectedFile.path));

        if (currentIndex == -1) {
            return null;
        }

        const {changes}= this.props.info;
        const filesReviewedByUser = this.props.reviewedFiles;

        let index = currentIndex;

        while (true) {
            index += direction;

            if(index == -1) {
                index = changes.length - 1;
            } else if(index == changes.length) {
                index = 0;
            }

            if (index == currentIndex) {
                return current;
            }

            const candidate = changes[index].path;

            if (!PathPairs.contains(filesReviewedByUser, candidate)) {
                return candidate;
            }
        }
    }

    render() {
        const { info, selectedFile, onSelectFileForView } = this.props;

        const menuItems = [];

        if (selectedFile) {
            const nextFile = this._findNextUnreviewedFile(selectedFile.path, 1);
            const prevFile = this._findNextUnreviewedFile(selectedFile.path, -1);

            menuItems.push(<Menu.Item fitted key="review-mark">
                <Popup
                    trigger={<ReviewMark reviewed={this.props.selectedFile.isReviewed} onClick={this._changeFileReviewState}/>}
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
                <Popup
                    trigger={<Icon onClick={() => onSelectFileForView(prevFile)} name="step backward" circular link />}
                    content="Previous unreviewed file"
                />
                <Popup
                
                    trigger={<Icon onClick={() => onSelectFileForView(nextFile)} name="step forward" circular link />}
                    content="Next unreviewed file"
                />
            </Menu.Item>);
            menuItems.push(<Menu.Item fitted key="file-path">
                <span className="file-path">{selectedFile.path.newPath}</span>
            </Menu.Item>);
        }

        return (
            <div ref={this._handleRef}>
                <Segment>
                    <Sticky context={this.state.stickyContainer} id="file-sticky">
                        <Menu secondary id="file-menu">
                            {menuItems}
                            <Menu.Menu position='right'>
                                <Menu.Item>
                                    <Button positive onClick={this.props.publishReview}>Publish Changes</Button>
                                    &nbsp;
                                    <ChangedFileTreePopup
                                        paths={info.changes.map(i => i.path)}
                                        selected={selectedFile ? selectedFile.path : emptyPathPair}
                                        reviewedFiles={this.props.reviewedFiles}
                                        onSelect={onSelectFileForView}
                                    />
                                </Menu.Item>
                            </Menu.Menu>
                        </Menu>
                    </Sticky>
                    <div>
                        {selectedFile ?
                            <FileView file={selectedFile} />
                            : <NoFileView />
                        }
                    </div>
                </Segment>
            </div>
        );
    }
}