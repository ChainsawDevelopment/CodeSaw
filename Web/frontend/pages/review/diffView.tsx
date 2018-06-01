import * as React from "react";
import { DiffChunk } from "../../api/reviewer";

import {Diff} from 'react-diff-view';
import 'react-diff-view/index.css';
import './diffView.less';

const splitCunks = (chunks: DiffChunk[]) => {
    let oldLineCounter = 1;
    let newLineCounter = 1;
    const result = [];

    for(const chunk of chunks) {
        let text = chunk.text;

        if(text.charAt(text.length - 1) == '\n') {
            text = text.substring(0, text.length - 1);
        }

        let type = '';
        switch(chunk.operation) {
            case 'Delete': 
                type = 'delete';
                break;
            case 'Equal':
                type = 'normal';
                break;
            case 'Insert':
                type = 'insert';
        }

        const lines = text.split('\n');

        const splittedChunk = {
            ...chunk,
            type: type,
            lines: []
        }

        for(const line of lines) {
            splittedChunk.lines.push({
                type: type,
                content: line,
                classification: chunk.classification,
                isNormal: type == 'normal',
                isDelete: type == 'delete',
                isInsert: type == 'insert',
                oldLineNumber: oldLineCounter,
                newLineNumber: newLineCounter,
                lineNumber: newLineCounter,
                classNames: 'my-class'
            });

            if(type != 'insert') {
                oldLineCounter ++;
            } 

            if (type != 'delete') {
                newLineCounter ++;
            }
        }

        result.push(splittedChunk);
    }

    return result;
}

const oppositeType = (type: string) => {
    switch(type) {
        case 'insert': return 'delete';
        case 'delete': return 'insert';
        default: return type;
    }
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

const diffView = (props: {chunks: DiffChunk[]}) => {
    const hunks = [];

    let hunk = {
        oldStart: 1,
        oldLines: 1,
        newStart: 1,
        newLines: 1,
        content: 'Hunk header',
        changes: []
    };

    const splitted = splitCunks(props.chunks);

    for(let i = 0; i < splitted.length; i++) {
        const chunk = splitted[i];

        if(chunk.type == 'normal') {
            hunk.changes = [...hunk.changes, ...chunk.lines];
            continue;
        }

        if(i == splitted.length - 1) {
            hunk.changes = [...hunk.changes, ...chunk.lines];
            continue;
        }

        const nextChunk = splitted[i + 1];

        if(oppositeType(chunk.type) == nextChunk.type) {
            const lines1 = chunk.lines;
            const lines2 = nextChunk.lines;
            const zipped = zipLines(lines1, lines2);

            hunk.changes = [...hunk.changes, ...zipped];
            
            i++;
        } else {
            hunk.changes = [...hunk.changes, ...chunk.lines];
        }
    }

    hunks.push(hunk);

    const events = {
        gutter: {
            onClick: (change) => {
                console.log('Clicked gutter', change)
            }
        }
    };

    return ( 
        <div>
            <Diff 
            viewType="split"
            diffType="modify"
            hunks={hunks}
            customEvents={events}
            />
        </div>
    );
};

export default diffView;