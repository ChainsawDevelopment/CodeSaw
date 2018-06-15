import * as React from "react";
import Menu from 'semantic-ui-react/dist/commonjs/collections/Menu';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
import Rail from 'semantic-ui-react/dist/commonjs/elements/Rail';
import Sticky from 'semantic-ui-react/dist/commonjs/modules/Sticky';
import { RevisionRangeInfo } from "../../api/reviewer";
import DiffView from './diffView';
import FileSummary from './fileSummary';
import ChangedFileTreePopup from "./fileTreePopup";
import { FileInfo } from "./state";
import ReviewMark from "./reviewMark";
import { PathPair, emptyPathPair } from "../../pathPair";


const FileView = (props: { file: FileInfo }) => {
    const { file } = props;
    
    return (
        <>
            <FileSummary file={file} />
            {file.diff ? <DiffView hunks={file.diff.hunks} /> : null}
        </>
    );
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

    render() {
        const { info, selectedFile, onSelectFileForView } = this.props;

        const menuItems = [];

        if (selectedFile) {
            menuItems.push(<Menu.Item key="review-mark">
                <ReviewMark reviewed={this.props.selectedFile.isReviewed} onClick={this._changeFileReviewState}/>
            </Menu.Item>);

            menuItems.push(<Menu.Item
                key="file-path"
                className="file-path"
                content={selectedFile.path.newPath} />
            );
            menuItems.push(<Menu.Item key="refresh-diff">
                <Button onClick={() => onSelectFileForView(selectedFile.path)}>Refresh diff</Button>
            </Menu.Item>);
        }

        return (
            <div ref={this._handleRef}>
                <Segment>
                    <Sticky context={this.state.stickyContainer} id="file-sticky">
                        <Menu secondary id="file-menu">
                            <Menu.Item key="file-selector">
                                <ChangedFileTreePopup
                                    paths={info.changes.map(i => i.path)}
                                    selected={selectedFile ? selectedFile.path : emptyPathPair}
                                    reviewedFiles={this.props.reviewedFiles}
                                    onSelect={onSelectFileForView}
                                />
                            </Menu.Item>
                            {menuItems}
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