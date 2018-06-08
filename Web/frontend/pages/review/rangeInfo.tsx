import * as React from "react";
import Menu from 'semantic-ui-react/dist/commonjs/collections/Menu';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
import Rail from 'semantic-ui-react/dist/commonjs/elements/Rail';
import Sticky from 'semantic-ui-react/dist/commonjs/modules/Sticky';
import { PathPair, RevisionRangeInfo, emptyPathPair } from "../../api/reviewer";
import DiffView from './diffView';
import FileSummary from './fileSummary';
import ChangedFileTreePopup from "./fileTreePopup";
import { FileInfo } from "./state";

export type SelectFileForViewHandler = (path: PathPair) => void;

export interface Props {
    info: RevisionRangeInfo;
    selectedFile: FileInfo;
    onSelectFileForView: SelectFileForViewHandler
}

export default class RangeInfo extends React.Component<Props, {stickyContainer: HTMLDivElement}> {
    constructor(props: Props) {
        super(props);
        this.state = {stickyContainer: null};
    }

    private _handleRef = el => {
        this.setState({stickyContainer: el});
    }

    render() {
        const { info, selectedFile, onSelectFileForView } = this.props;

        return (
            <div ref={this._handleRef}>
                <Segment>
                    <Sticky context={this.state.stickyContainer}>
                        <Menu secondary id="file-menu">
                            <Menu.Item>
                                <ChangedFileTreePopup
                                    paths={info.changes.map(i => i.path)}
                                    selected={selectedFile ? selectedFile.path : emptyPathPair}
                                    onSelect={onSelectFileForView}
                                />
                            </Menu.Item>
                            <Menu.Item>
                                <Button onClick={() => onSelectFileForView(selectedFile.path)}>Refresh diff</Button>
                            </Menu.Item>
                        </Menu>
                    </Sticky>
                    <div>                    
                        {selectedFile ? <FileSummary file={selectedFile} /> : null}
                        {selectedFile && selectedFile.diff ? <DiffView hunks={selectedFile.diff.hunks} /> : null}
                    </div>
                </Segment>
            </div>
        );
    }
}