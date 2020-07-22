import { Hunk } from '@api/reviewer';
import * as React from 'react';
import Divider from '@ui/elements/Divider';

const HunkHeader = (props: { hunk: Hunk }): JSX.Element => {
    const { hunk } = props;

    const content = `Hunk ${hunk.newPosition.start} - ${hunk.newPosition.end} (${hunk.newPosition.length} lines)`;

    return (
        <div style={{ width: '100%' }}>
            <Divider key={hunk.oldPosition.start} horizontal>
                @{hunk.oldPosition.start},{hunk.newPosition.start} - {hunk.oldPosition.end},{hunk.newPosition.end} (
                {hunk.oldPosition.length},{hunk.newPosition.length} lines)
            </Divider>
        </div>
    );
};

export default HunkHeader;
