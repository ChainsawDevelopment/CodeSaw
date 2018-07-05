import { FileInfo } from "./state";
import * as React from "react";
import Message from '@ui/collections/Message';

export default (props: {file: FileInfo}): JSX.Element => {
    const items: JSX.Element[] = [];

    if (props.file.treeEntry && props.file.treeEntry.renamedFile) {
        const { path } = props.file.treeEntry;

        items.push(
            <div key="renamed" className="renamed">File renamed <pre>{path.oldPath}</pre> &rarr; <pre>{path.newPath}</pre></div>
        );
    }

    if(items.length == 0) {
        return null;
    }

    return (
        <Message className="file-summary">
            <Message.Content>
                {items}
            </Message.Content>
        </Message>
    );
};