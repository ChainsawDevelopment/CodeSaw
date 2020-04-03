import * as React from 'react';
import classNames from 'classnames';
const style = require('./RangeSelector.less');

const Block = (props: {
    index: number;
    isSelecting: boolean;
    ends: [number, number];
    startSelecting: () => void;
    stopSelecting: () => void;
    selectOver: () => void;
    children: any;
}): JSX.Element => {
    const onDown = (e: React.SyntheticEvent) => {
        props.startSelecting();
        e.preventDefault();
    };

    const onUp = () => {
        props.stopSelecting();
    }

    function whenSelecting<T>(value: T): T {
        if (props.isSelecting) {
            return value;
        } else {
            return null;
        }
    };

    const onOver = whenSelecting(() => {
        props.selectOver();
    });

    return <div
        onMouseDown={onDown}
        onMouseOver={onOver}
        onMouseUp={onUp}
        className={classNames({
            'selector-item': true,
            'start': props.index === props.ends[0],
            'end': props.index === props.ends[1],
            'middle': props.ends[0] < props.index && props.index < props.ends[1]
        })}
    ><div className="wrapper">{props.children}</div></div>;
}

interface Selection {
    inProgress: boolean;
    end1: number;
    end2: number;
}

const sortEnds = (e1: number, e2: number): [number, number] => {
    return [
        Math.min(e1, e2),
        Math.max(e1, e2),
    ];
}

const RangeSelector = (props: { children: any; start: number; end: number; onChange: (start: number, end: number) => void; }): JSX.Element => {
    const [selection, setSelection] = React.useState<Selection>({
        inProgress: false,
        end1: null,
        end2: null
    });

    const ends = sortEnds(
        selection.end1 === null ? props.start : selection.end1,
        selection.end2 === null ? props.end : selection.end2
    );

    const startSelecting = (index: number) => {
        document.onmouseup = () => stopSelecting(index);
        setSelection({
            inProgress: true,
            end1: index,
            end2: index
        });
    };

    const previewSelection = (index: number) => {
        document.onmouseup = () => stopSelecting(index);
        setSelection({
            ...selection,
            inProgress: true,
            end2: index
        });
    };

    const stopSelecting = (index: number) => {
        document.onmouseup = null;
        setSelection({
            inProgress: false,
            end1: null,
            end2: null
        });
        const ends = sortEnds(selection.end1, index);
        props.onChange(ends[0], ends[1]);
    };

    const blocks = React.Children.map(props.children, (c, i) => <Block
        key={i}
        index={i}
        isSelecting={selection.inProgress}
        ends={ends}
        startSelecting={startSelecting.bind(null, i)}
        selectOver={previewSelection.bind(this, i)}
        stopSelecting={stopSelecting.bind(this, i)}
    >{c}</Block>);

    return <div>
        <div className="selector">
            {blocks}
        </div>
    </div>
}

export default RangeSelector;