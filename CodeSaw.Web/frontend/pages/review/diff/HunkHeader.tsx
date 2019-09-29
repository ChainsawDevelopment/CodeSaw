import { Hunk } from "@api/reviewer";
import * as React from "react";
import Divider from "@ui/elements/Divider";

const HunkHeader = (props: {hunk: Hunk}) => {
    const { hunk } = props;

    const content = `Hunk ${hunk.newPosition.start + 1} - ${hunk.newPosition.end + 1} (${hunk.newPosition.length} lines)`

    return <div style={{width: '100%'}}>
        <Divider key={hunk.oldPosition.start} horizontal>
            @{hunk.oldPosition.start + 1},{hunk.newPosition.start + 1} - {hunk.oldPosition.end + 1},{hunk.newPosition.end + 1} ({hunk.oldPosition.length},{hunk.newPosition.length} lines)
        </Divider>
    </div>;
}

export default HunkHeader;