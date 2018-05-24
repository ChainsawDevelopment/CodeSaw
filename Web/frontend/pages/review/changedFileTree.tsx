import * as React from "react";
import List from 'semantic-ui-react/dist/commonjs/elements/List';

const FileItem = (props: { path: string, isSelected: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<List.Header className='selected-file'>{props.path}</List.Header>)
    } else {
        header = (<List.Header as='a' onClick={props.onclick}>{props.path}</List.Header>);
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

const changedFileTree = (props: { paths: string[], selected:string, onSelect: (path:string) => void }) => {
    const items = props.paths.map(p => (
        <FileItem 
            key={p} 
            path={p} 
            isSelected={p == props.selected} 
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