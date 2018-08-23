import * as React from "react";
import { Hunk, FileDiff } from "../../api/reviewer";

import { Diff, Hunk as DiffHunk, tokenize, markEdits, getChangeKey, expandCollapsedBlockBy, expandFromRawCode, getCorrespondingOldLineNumber  } from 'react-diff-view';
import BinaryDiffView from './binaryDiffView';
import 'react-diff-view/style/index.css';
import './diffView.less';
import * as classNames from "classnames";

interface Change {
    oldLineNumber: number;
    newLineNumber: number;

    isDelete: boolean;
    isInsert: boolean;

    [key:string]: any;
}

const mapHunkToView = (hunk: Hunk) => {
    var changes: Change[] = [];

    let oldLineCounter = hunk.oldPosition.start + 1;
    let newLineCounter = hunk.newPosition.start + 1;

    for (const line of hunk.lines) {
        let type = '';
        switch (line.operation) {
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

        if (type != 'insert') {
            oldLineCounter++;
        }

        if (type != 'delete') {
            newLineCounter++;
        }
    }

    const viewHunk = {
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
    switch (type) {
        case 'insert': return 'delete';
        case 'delete': return 'insert';
        default: return type;
    }
}

const zipChanges = (changes: Change[]): Change[] => {
    let result = [];

    let inserts = [];
    let deletes = [];

    for (const change of changes) {
        if (change.isInsert) {
            inserts.push(change);
            continue;
        }

        if (change.isDelete) {
            deletes.push(change);
            continue;
        }

        if (change.isNormal) {
            const zipped = zipLines(deletes, inserts);

            result = result.concat(zipped);
            inserts = [];
            deletes = [];

            result.push(change);
        }
    }

    if (inserts.length > 0 || deletes.length > 0) {
        const zipped = zipLines(deletes, inserts);

        result = result.concat(zipped);
    }

    return result;
}

const zipLines = <T extends {}>(lines1: T[], lines2: T[]): T[] => {
    const result: T[] = [];

    const minLength = Math.min(lines1.length, lines2.length);

    for (let i = 0; i < minLength; i++) {
        result.push(lines1[i]);
        result.push(lines2[i])
    }

    for (let i = minLength; i < lines1.length; i++) {
        result.push(lines1[i]);
    }

    for (let i = minLength; i < lines2.length; i++) {
        result.push(lines2[i]);
    }

    return result;
}

export type DiffSide = 'left' | 'right';

export interface LineWidget {
    side: DiffSide;
    lineNumber: number;
    widget: JSX.Element;
}

export type DiffType = 'modify' | 'add' | 'delete';

export interface Props {
    diffInfo: FileDiff;
    lineWidgets: LineWidget[];
    type: DiffType;
    onLineClick?: (side: DiffSide, line: number) => void;
    contents: {
        previous: string;
        current: string;
    };
}

const leftSideMatch = (change: Change, lineNumber: number) => {
    if (change.isInsert) {
        return false;
    }

    if (change.oldLineNumber != lineNumber) {
        return false;
    }

    return true;
}

const rightSideMatch = (change: Change, lineNumber: number) => {
    if (change.isDelete) {
        return false;
    }

    if (change.newLineNumber != lineNumber) {
        return false;
    }

    return true;
}

const getFullChangeKey = change => {
    if (!change) {
        throw new Error('change is not provided');
    }

    const {isNormal, isInsert, oldLineNumber, newLineNumber} = change;

    let prefix = '';

    if (isNormal) {
        prefix = 'N';
    } else if (isInsert) {
        prefix = 'I';
    } else {
        prefix = 'D';
    }

    return prefix + oldLineNumber + '_' + newLineNumber;
};

const diffView = (props: Props) => {
    if (props.diffInfo.isBinaryFile) {
        return (<BinaryDiffView diffInfo={props.diffInfo} />)
    }

    let viewHunks = props.diffInfo.hunks.map(mapHunkToView);

    if (viewHunks.length == 0) {
        viewHunks.push({
            oldStart: 1,
            oldLines: 0,
            newStart: 1,
            newLines: 1,
            content: '',
            changes: []
        });
    }

    // expands all hunks that matches condition
    // however condition is just `false` so nothing will be expanded
    // once we start working on expanding/collapsing hunks this will be useful
    viewHunks = expandCollapsedBlockBy(viewHunks, props.contents.current, () => false);

    for (let widget of props.lineWidgets) {
        const matchingHunk = viewHunks.findIndex(i =>
            (widget.side == 'left' && i.oldStart <= widget.lineNumber && widget.lineNumber <= i.oldStart + i.oldLines)
            || (widget.side == 'right' && i.newStart <= widget.lineNumber && widget.lineNumber <= i.newStart + i.newLines)
        );

        if (matchingHunk >= 0) {
            continue;
        }

        let lineNumber = widget.lineNumber;

        if (widget.side == 'right') {
            lineNumber = getCorrespondingOldLineNumber(viewHunks, lineNumber);
        }

        viewHunks = expandFromRawCode(viewHunks, props.contents.current, lineNumber - 2, lineNumber + 2);
    }

    const events = {
        gutterEvents: {
            onClick: change => {
                console.log('click', change);
                if(props.onLineClick) {
                    const lineNumber = change.newLineNumber;
                    props.onLineClick('right', lineNumber); // TODO: detect side
                }
            }
        }
    };

    let widgets = {};

    for (let item of props.lineWidgets) {
        const match = item.side == 'left' ? leftSideMatch : rightSideMatch;

        for (let hunk of viewHunks) {
            for (let change of hunk.changes) {
                if( !match(change, item.lineNumber)) {
                    continue;
                }

                widgets[getFullChangeKey(change) + '_' + item.side[0].toUpperCase()] = item.widget;

                break;
            }
        }
    }

    const tokens = tokenize(viewHunks, {
        oldSource: props.contents.previous,
        enhancers: [
            markEdits(viewHunks)
        ]
    });

    return (
        <Diff
            viewType="split"
            diffType={props.type}
            widgets={widgets}
            tokens={tokens}
        >
        {viewHunks.map((h, i) => <DiffHunk key={i} hunk={h} gutterEvents={events.gutterEvents}/>)}
        </Diff>
    );
};

export default diffView;
