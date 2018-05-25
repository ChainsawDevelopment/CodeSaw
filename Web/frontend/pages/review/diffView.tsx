import * as React from "react";
import { DiffChunk } from "../../api/reviewer";
import * as classNames from 'classnames';

import "./diffView.less";

const splitLines = (chunks: DiffChunk[]): DiffChunk[] => {
    let result: DiffChunk[] = [];

    for (const chunk of chunks) {
        let text = chunk.text;

        if(text.charAt(text.length - 1) == '\n') {
            text = text.substring(0, text.length - 2);
        }

        const lines = text.split('\n');

        const lineChunks = lines.map(l => ({
            classification: chunk.classification,
            operation: chunk.operation,
            text: l
        }));

        result = result.concat(lineChunks);
    }

    return result;
}

const Side = (props: {lineNo: number; className: string; text: string}) => {
    return (
        <div className={props.className}>
            <div className="line-counter">{props.lineNo}</div>
            <span className="text">{props.text}&nbsp;</span>
        </div>
    );
}

const Line = (props: {lineNo: number; lineChunk: DiffChunk}) => {
    const { operation, text } = props.lineChunk;

    const leftClasses = classNames({
        left: true,
        equal: operation == 'equal',
        delete: operation == 'delete',
        'base-change': props.lineChunk.classification == 'base'
    });

    const rightClasses = classNames({
        right: true,
        equal: operation == 'equal',
        insert: operation == 'insert',
        'base-change': props.lineChunk.classification == 'base'
    });    

    const leftText = operation != 'insert' ? text : '';
    const rightText = operation != 'delete' ? text : '';

    return (
        <div className="line">
            <Side lineNo={props.lineNo} className={leftClasses} text={leftText} />
            <Side lineNo={props.lineNo} className={rightClasses} text={rightText} />
        </div>
    );
}

const generateLineByLineDiff = (chunks: DiffChunk[]): JSX.Element[] => {
    const lineChunks = splitLines(chunks);

    return lineChunks.map((line, i) => (<Line key={i} lineNo={i + 1} lineChunk={line} />))
};

const diffView = (props: {chunks: DiffChunk[]}): JSX.Element => {
    const lineByLine = generateLineByLineDiff(props.chunks);
    
    const items = lineByLine.map((line, i) => (
        <div key={i} className="line">
            <div className="left">{line[0]}&nbsp;</div>
            <div className="right">{line[1]}&nbsp;</div>
        </div>
    ));

    return (
        <div className="diff-view">
            {lineByLine}
        </div>
    )
};

export default diffView;