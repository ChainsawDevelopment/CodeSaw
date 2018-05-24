import * as React from "react";
import List from 'semantic-ui-react/dist/commonjs/elements/List';

const FileItem = (props: {path: string}) => {
    return (
        <List.Item>
            <List.Icon name='file' />
            <List.Content>
                <List.Header>{props.path}</List.Header>
            </List.Content>
        </List.Item>
    );
}

const changedFileTree = (props: {paths: string[]}) => {
    const items = props.paths.map(p => (
        <FileItem key={p} path={p} />
    ));

    return (
        <List>
            {items}
        </List>
    );
};

export default changedFileTree;