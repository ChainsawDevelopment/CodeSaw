import * as React from "react";
import List from 'semantic-ui-react/dist/commonjs/elements/List';
import { PathPair } from "../../api/reviewer";

const FileItem = (props: { path: PathPair, isSelected: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<List.Header className='selected-file'>{props.path.newPath}</List.Header>)
    } else {
        header = (<List.Header as='a' onClick={props.onclick}>{props.path.newPath}</List.Header>);
    }

    return (
        <List.Item>
            <List.Icon name='file' />
            <List.Content>
                {header}
            </List.Content>
        </List.Item>
    );
}

const changedFileTree = (props: { paths: PathPair[], selected:PathPair, onSelect: (path: PathPair) => void }) => {
    const items = props.paths.map(p => (
        <FileItem 
            key={p.newPath} 
            path={p} 
            isSelected={p.newPath == props.selected.newPath} 
            onclick={() => props.onSelect(p)}
        />
    ));

    return (
        <List className="file-tree">
            {items}
        </List>
    );
};

export default changedFileTree;