import * as React from "react";
import { FileDiff } from "../../api/reviewer";

const binaryDiffView = (props: { diffInfo: FileDiff }) => {
    const previousSize = props.diffInfo.binarySizes.previousSize;
    const currentSize = props.diffInfo.binarySizes.currentSize;
    if (props.diffInfo.areBinaryEqual) {
        return (
            <div>
                Binary files are the same.<br />
                Size: {currentSize}
            </div>
        );
    }

    return (
        <div>
            Binary files are different.<br />
            Previous size: {previousSize} bytes.<br />
            Current size: {currentSize} bytes.
        </div>
    );
};

export default binaryDiffView;