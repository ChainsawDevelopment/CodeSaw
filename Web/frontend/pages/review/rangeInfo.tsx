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

const FileView = (props: {file:FileInfo}) => {
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

        const menuItems = [];

        if(selectedFile) {
            menuItems.push(<Menu.Item 
                key="file-path"
                className="file-path"
                content={selectedFile.path.newPath}/>
            );
            menuItems.push(<Menu.Item key="refresh-diff">
                <Button onClick={() => onSelectFileForView(selectedFile.path)}>Refresh diff</Button>
            </Menu.Item>);
        }

        return (
            <div ref={this._handleRef}>
                <Segment>
                    <Sticky context={this.state.stickyContainer}>
                        <Menu secondary id="file-menu">
                            <Menu.Item key="file-selector">
                                <ChangedFileTreePopup
                                    paths={info.changes.map(i => i.path)}
                                    selected={selectedFile ? selectedFile.path : emptyPathPair}
                                    onSelect={onSelectFileForView}
                                />
                            </Menu.Item>
                            {menuItems}
                        </Menu>
                    </Sticky>
                    <div>  
                        {selectedFile ?                   
                            <FileView file={selectedFile}/>
                            : <NoFileView />
                        }
                    </div>
                </Segment>
            </div>
        );
    }
}