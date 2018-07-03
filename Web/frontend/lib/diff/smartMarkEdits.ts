import { markWordEdits } from 'react-diff-view';

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

export default () => {
    const markEdits = markWordEdits();
    return (a,b) => {
        let result = markEdits(a, b);

        result = collapse(result);

        return result;
    };
}