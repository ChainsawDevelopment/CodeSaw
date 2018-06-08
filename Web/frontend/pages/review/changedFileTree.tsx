import * as React from "react";
import List from 'semantic-ui-react/dist/commonjs/elements/List';
import Icon from 'semantic-ui-react/dist/commonjs/elements/Icon';
import { PathPair } from "../../api/reviewer";
import { SemanticCOLORS, SemanticICONS } from "semantic-ui-react/dist/commonjs";

const FileItem = (props: { path: PathPair, isSelected: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<span className='selected-file'>{props.path.newPath}</span>)
    } else {
        header = (<a onClick={props.onclick}>{props.path.newPath}</a>);
    }

    let markColor:SemanticCOLORS;
    let markIcon:SemanticICONS;

    if (props.isSelected) {
        markColor = 'green';
        markIcon = 'eye slash' as SemanticICONS;
    } else {
        markColor = 'red';
        markIcon = 'eye';
    }

    return (
        <List.Item className="file-tree-item">
            <List.Content>
                <Icon name={markIcon} color={markColor}/>
                <span>{header}</span>
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