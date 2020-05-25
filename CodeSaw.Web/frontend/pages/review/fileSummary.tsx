import { FileInfo } from "./state";
import { FileToReview } from "../../api/reviewer"
import * as React from "react";
import Message from '@ui/collections/Message';
import FriendlyRevisionId from "@src/components/FriendlyRevisionId";

const describeFileOperations = (treeEntry: FileToReview): JSX.Element => {
    const { changeType, diffFile:path } = treeEntry;

    if (changeType == 'renamed') {
        return (
            <div key="renamed" className="file-operations">File renamed: <pre>{path.oldPath}</pre> &rarr; <pre>{path.newPath}</pre></div>
        );
    }
    else if (changeType == 'deleted') {
        return (
            <div key="deleted" className="file-operations">File deleted: <pre>{path.oldPath}</pre></div>
        );
    }
    else if (changeType == 'created') {
        return (
            <div key="created" className="file-operations">File created: <pre>{path.newPath}</pre></div>
        );
    } else {
        return null;
    }
}

export default (props: {file: FileInfo}): JSX.Element => {
    const items: JSX.Element[] = [];

    {
        const item = describeFileOperations(props.file.fileToReview)
        if(item != null)
            items.push(item);
    }

    if(items.length == 0) {
        return null;
    }

    return (
        <Message className="file-summary">
            <Message.Content>
                <div>Show diff
                    <strong> <FriendlyRevisionId revision={props.file.fileToReview.previous}/> </strong>
                    to <strong> <FriendlyRevisionId revision={props.file.fileToReview.current}/> </strong>
                </div>
                {items}
            </Message.Content>
        </Message>
    );
};