import { FileInfo } from "./state";
import { ChangedFile } from "../../api/reviewer"
import * as React from "react";
import Message from '@ui/collections/Message';

const describeFileOperations = (treeEntry: ChangedFile): JSX.Element => {
    const { path } = treeEntry;

    if (treeEntry.renamedFile) {
        return (
            <div key="renamed" className="file-operations">File renamed: <pre>{path.oldPath}</pre> &rarr; <pre>{path.newPath}</pre></div>
        );
    }
    else if (treeEntry.deletedFile) {
        return (
            <div key="deleted" className="file-operations">File deleted: <pre>{path.oldPath}</pre></div>
        );
    }
    else if (treeEntry.newFile) {
        return (
            <div key="created" className="file-operations">File created: <pre>{path.newPath}</pre></div>
        );
    }
}

export default (props: {file: FileInfo}): JSX.Element => {
    const items: JSX.Element[] = [];

    // if (props.file.treeEntry)
    // {
    //     items.push(describeFileOperations(props.file.treeEntry));
    // } 

    if(items.length == 0) {
        return null;
    }

    return (
        <Message className="file-summary">
            <Message.Content>
                <div>Show diff 
                    <strong> <pre style={{display: 'inline'}}>{props.file.fileToReview.previous}</pre> </strong>
                    to <strong> <pre style={{display: 'inline'}}>{props.file.fileToReview.current}</pre> </strong>
                    changes: <strong>{props.file.fileToReview.hasChanges ? "YES" : "NO"}</strong> 
                </div>
                {items}
            </Message.Content>
        </Message>
    );
};