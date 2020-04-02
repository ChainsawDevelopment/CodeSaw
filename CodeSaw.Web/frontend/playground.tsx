import * as React from 'react';
import Label from '@ui/elements/Label';
import Button from '@ui/elements/Button';
import RangeSelector from './components/RangeSelector';



const Playground = (): JSX.Element => {
    const [range, setRange] = React.useState([1, 2]);
    console.log(range);

    const items = [1, 2, 3, 4, 5, 6].map(n => <Label key={n} size="tiny">{n}</Label>);

    return <div>
        <RangeSelector start={range[0]} end={range[1]} onChange={(s, e) => setRange([s, e])}>
            {items}
        </RangeSelector>
        <Button onClick={() => setRange([range[0] - 1, range[1]])}>Left</Button>
        <Button onClick={() => setRange([range[0], range[1] + 1])}>Right</Button>
    </div>
}

export default Playground;