import * as React from "react";
import { Hunk } from "../../api/reviewer";

import {Diff, markWordEdits} from 'react-diff-view';
import 'react-diff-view/index.css';
import './diffView.less';
import * as classNames from "classnames";

const mapHunkToView = (hunk: Hunk) => {    
    var changes = [];

    let oldLineCounter = hunk.oldPosition.start + 1;
    let newLineCounter = hunk.newPosition.start + 1;

    for (const line of hunk.lines) {
        let type = '';
        switch(line.operation) {
            case 'Delete': 
                type = 'delete';
                break;
            case 'Equal':
                type = 'normal';
                break;
            case 'Insert':
                type = 'insert';
                break;
            default:
                console.log('Unknown', line.operation);
        }

        changes.push({
            type: type,
            content: line.text,
            isNormal: type == 'normal',
            isDelete: type == 'delete',
            isInsert: type == 'insert',
            oldLineNumber: oldLineCounter,
            newLineNumber: newLineCounter,
            lineNumber: type == 'delete' ? oldLineCounter : newLineCounter,
            classNames: classNames({
                'base-change': line.classification == 'BaseChange' && line.operation != 'Equal',
                'review-change': line.classification == 'ReviewChange' && line.operation != 'Equal'
            })
        });

        if(type != 'insert') {
            oldLineCounter ++;
        } 

        if (type != 'delete') {
            newLineCounter ++;
        }
    }

    let viewHunk = {
        oldStart: hunk.oldPosition.start + 1,
        oldLines: hunk.oldPosition.length + 1,
        newStart: hunk.newPosition.start + 1,
        newLines: hunk.newPosition.length + 1,
        content: `Hunk ${hunk.newPosition.start + 1} - ${hunk.newPosition.end + 1} (${hunk.newPosition.length} lines)`,
        changes: zipChanges(changes)
    };

    return viewHunk;
};

const oppositeType = (type: string) => {
    switch(type) {
        case 'insert': return 'delete';
        case 'delete': return 'insert';
        default: return type;
    }
}

const zipChanges = (changes: any[]) => {
    let result = [];

    let inserts = [];
    let deletes = [];

    for (const change of changes) {
        if(change.isInsert) {
            inserts.push(change);
            continue;
        }

        if(change.isDelete) {
            deletes.push(change);
            continue;
        }

        if(change.isNormal) {
            const zipped = zipLines(deletes, inserts);

            result = result.concat(zipped);            
            inserts = [];
            deletes = [];

            result.push(change);
        }
    }

    if(inserts.length > 0 ||deletes.length > 0) {
        const zipped = zipLines(deletes, inserts);

        result = result.concat(zipped);    
    }

    return result;
}

const zipLines = <T extends {}>(lines1: T[], lines2: T[]): T[] => {
    const result:T[] = [];

    const minLength = Math.min(lines1.length, lines2.length);

    for(let i = 0; i < minLength; i++) {
        result.push(lines1[i]);
        result.push(lines2[i])
    }

    for(let i = minLength; i < lines1.length; i++) {
        result.push(lines1[i]);
    }

    for(let i = minLength; i < lines2.length; i++) {
        result.push(lines2[i]);
    }

    return result;
}

const diffView = (props: {hunks: Hunk[]}) => {
    const viewHunks = props.hunks.map(mapHunkToView);

    const events = {
        gutter: {
            onClick: (change) => {
                console.log('Clicked gutter', change)
            }
        }
    };

    // const widgets = {
    //     'I19': (
    //         <span className="error">Line too long</span>
    //     )
    // };

    const markEdits = markWordEdits();

    return ( 
        <div>
            <Diff 
            viewType="split"
            diffType="modify"
            hunks={viewHunks}
            customEvents={events}
            markEdits={markEdits}
            />
        </div>
    );
};

export default diffView;