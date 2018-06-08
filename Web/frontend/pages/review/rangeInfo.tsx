import * as React from "react";
import Menu from 'semantic-ui-react/dist/commonjs/collections/Menu';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
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

export default (props: Props): JSX.Element => {
    return (
        <div style={{ flex: 1 }}>
            <Menu secondary>
                <Menu.Item>
                    <ChangedFileTreePopup
                        paths={props.info.changes.map(i => i.path)}
                        selected={props.selectedFile ? props.selectedFile.path : emptyPathPair}
                        onSelect={props.onSelectFileForView}
                    />
                </Menu.Item>
                <Menu.Item>
                    <Button onClick={() => props.onSelectFileForView(props.selectedFile.path)}>Refresh diff</Button>
                </Menu.Item>
            </Menu>
            <div>
                
            </div>
            <Segment basic>
                
                {props.selectedFile ? <FileSummary file={props.selectedFile} /> : null}
                {props.selectedFile && props.selectedFile.diff ? <DiffView hunks={props.selectedFile.diff.hunks} /> : null}
            </Segment>
        </div>
    );
}