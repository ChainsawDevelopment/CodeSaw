import { Revision2Id } from "@api/revisionId";
import * as React from "react";

const toFriendly = (r:Revision2Id) => {
    if(Revision2Id.isBase(r)) {
        return 'base';
    }
    if(Revision2Id.isSelected(r)) {
        return r.revision.toString();
    }
    if(Revision2Id.isHash(r)) {
        return `HASH: ${r.head}`;
    }
    if(Revision2Id.isProvisional(r)) {
        return 'provisional';
    }
}

const FriendlyRevisionId = (props: {revision: Revision2Id}) => {
    return <pre style={{display: 'inline'}}>{toFriendly(props.revision)}</pre>
}

export default FriendlyRevisionId;