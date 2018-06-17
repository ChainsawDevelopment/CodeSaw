import * as React from "react";

import "./versionSelector.less";
import Radio from '@ui/addons/Radio';
import * as classNames from "classnames";
import { RevisionRange, RevisionId } from "../../api/reviewer";

type SelectRangeHandle = (range: RevisionRange) => void;

interface SingleSelectorProps {
    revisionId: RevisionId;
    range: RevisionRange;
    side: 'previous' | 'current';
    onSelectRange: SelectRangeHandle;
}

const SingleSelector = (props: SingleSelectorProps) => {
    return (<td>    
        <Radio 
            checked={props.revisionId == props.range[props.side]} 
            value={props.revisionId} 
            onChange={() => props.onSelectRange(
                { ...props.range, [props.side]: props.revisionId }
            )} />
    </td>)
}

interface Props {
    available: RevisionId[];
    hasProvisonal: boolean;
    range: RevisionRange;
    onSelectRange: SelectRangeHandle
}

const revisionTitle = (r: RevisionId):string => {
    switch(r) {
       case 'base':
            return '&perp;'
        case 'provisional':
            return 'P';
        default:
            return r.toString();
    }
}

const revisionClass = (r: RevisionId): string => classNames({
    base: r == 'base',
    provisional: r == 'provisional'
})

const versionSelector = (props: Props): JSX.Element => {
    const header = props.available.map(r => (<th 
        key={r} 
        className={revisionClass(r)}
        dangerouslySetInnerHTML={{__html: revisionTitle(r)}}></th>
    ));  
    const previous = props.available.map(r => (
        <SingleSelector key={r} revisionId={r} range={props.range} side='previous' onSelectRange={props.onSelectRange} />
    ));

    const current = props.available.map(r => (
        <SingleSelector key={r} revisionId={r} range={props.range} side='current' onSelectRange={props.onSelectRange} />
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