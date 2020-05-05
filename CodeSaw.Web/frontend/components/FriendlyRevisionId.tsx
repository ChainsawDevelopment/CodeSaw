import { RevisionId } from "@api/revisionId";
import * as React from "react";

const toFriendly = (r:RevisionId) => {
    if(RevisionId.isBase(r)) {
        return 'base';
    }
    if(RevisionId.isSelected(r)) {
        return r.revision.toString();
    }
    if(RevisionId.isHash(r)) {
        return `HASH: ${r.head}`;
    }
    if(RevisionId.isProvisional(r)) {
        return 'provisional';
    }
}

const FriendlyRevisionId = (props: {revision: RevisionId}) => {
    return <pre style={{display: 'inline'}}>{toFriendly(props.revision)}</pre>
}

export default FriendlyRevisionId;