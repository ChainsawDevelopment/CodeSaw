import * as React from "react";

import "./versionSelector.less";
import Radio from 'semantic-ui-react/dist/commonjs/addons/Radio';
import { RevisionRange } from "./state";

type SelectVersionHandler = (revision: number) => void;
type SelectRangeHandle = (range: RevisionRange) => void;

interface Props {
    available: number[];
    range: RevisionRange;
    onSelectRange: SelectRangeHandle
}

const versionSelector = (props: Props): JSX.Element => {
    const header = props.available.map(r => (<th key={r}>{r}</th>));  
    const previous = props.available.map(r => (
        <td key={r}>    
            <Radio 
                checked={r == props.range.previous} 
                value={r} 
                onChange={() => props.onSelectRange(
                    { ...props.range, previous: r }
                )} />
        </td>
    ));

    const current = props.available.map(r => (
        <td key={r}>    
            <Radio 
                checked={r == props.range.current} 
                value={r} 
                onChange={() => props.onSelectRange(
                    { ...props.range, current: r }
                )} />
        </td>
    ));

    return (
        <div className="version-selector">
            <table>
                <thead>
                    <tr>
                        <th>&nbsp;</th>
                        {header}
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Previous:</td>
                        {previous}
                    </tr>
                    <tr>
                        <td>Current:</td>
                        {current}
                    </tr>
                </tbody>
            </table>
        </div>
    );
};

export default versionSelector;