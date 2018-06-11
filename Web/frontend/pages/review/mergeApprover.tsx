import * as React from "react";

import "./mergeApprover.less";
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Checkbox from 'semantic-ui-react/dist/commonjs/modules/Checkbox';
import * as classNames from "classnames";
import { RevisionRange, RevisionId } from "../../api/reviewer";

type SelectRangeHandle = (range: RevisionRange) => void;

interface SingleSelectorProps {
    revisionId: RevisionId;
    range: RevisionRange;
    side: 'previous' | 'current';
    onSelectRange: SelectRangeHandle;
}

// const SingleSelector = (props: SingleSelectorProps) => {
//     return (<td>    
//         <Radio 
//             checked={props.revisionId == props.range[props.side]} 
//             value={props.revisionId} 
//             onChange={() => props.onSelectRange(
//                 { ...props.range, [props.side]: props.revisionId }
//             )} />
//     </td>)
// }

interface Props {
    available?: RevisionId[];
    hasProvisonal?: boolean;
    range?: RevisionRange;
    onSelectRange?: SelectRangeHandle
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

const mergeApprover = (props: Props): JSX.Element => {
    // const header = props.available.map(r => (<th 
    //     key={r} 
    //     className={revisionClass(r)}
    //     dangerouslySetInnerHTML={{__html: revisionTitle(r)}}></th>
    // ));  
    // const previous = props.available.map(r => (
    //     <SingleSelector key={r} revisionId={r} range={props.range} side='previous' onSelectRange={props.onSelectRange} />
    // ));

    // const current = props.available.map(r => (
    //     <SingleSelector key={r} revisionId={r} range={props.range} side='current' onSelectRange={props.onSelectRange} />
    // ));
    const onButtonClick = (): void => {
        console.log('merge!');
    }

    return (
        <div className="merge-approver">
            <Checkbox 
                label= { "Remove source branch" } 
            />
            <Button id= { "merge-button" } 
                onClick = { (e, v ) => onButtonClick() }            >Merge</Button>
        </div>
    );
};

export default mergeApprover;