import { markWordEdits } from 'react-diff-view';
import { EINTR } from 'constants';

interface Change {
    content: string;
}

type ChangeSpan = [number, number];

const collapseSide = (edits: ChangeSpan[]) => {
    if (edits.length < 2) {
        return edits;
    }

    const result = [edits[0]];
    
    for(let i = 1; i < edits.length; i++) {
        let lastSpan = result[result.length - 1];
        const edit = edits[i];

        const lastSpanEnd = lastSpan[0] + lastSpan[1];

        const distanceFromLast = edit[0] - lastSpanEnd;

        if(distanceFromLast <= 3) {
            lastSpan[1] += distanceFromLast + edit[1];
        } else {
            result.push(edit);
        }
    }

    return result;
}

const collapse = (edits: [ChangeSpan[], ChangeSpan[]]) => {
    return [
        collapseSide(edits[0]),
        collapseSide(edits[1])
    ];
}

interface TrimResult {
    change: Change;
    startOffset: number;
}

const trimChange = (change: Change): TrimResult => {
    if (change === null) {
        return {
            change,
            startOffset: 0
        };
    }

    const trimmedLeft = change.content.trimLeft();
    const startOffset = change.content.length - trimmedLeft.length;

    return {
        change: {
            ...change,
            content: trimmedLeft
        },
        startOffset: startOffset
    };
}

const untrim = (trim: TrimResult, edits: ChangeSpan[]) => {
    if (trim.change == null || trim.startOffset == 0) {
        return edits;
    }

    for (let edit of edits) {
        edit[0] += trim.startOffset;
    }

    return edits;
}

export default () => {
    const markEdits = markWordEdits();
    return (oldChange, newChange) => {
        const oldTrim = trimChange(oldChange);
        const newTrim = trimChange(newChange);

        let result = markEdits(oldTrim.change, newTrim.change);

        result = collapse(result);

        return [
            untrim(oldTrim, result[0]),
            untrim(newTrim, result[1]),
        ];
    };
}