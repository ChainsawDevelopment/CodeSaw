import * as React from 'react';
import { FileDiff } from '../../api/reviewer';

const imageDiffView = (props: { diffInfo: FileDiff }): JSX.Element => {
    const previousSize = props.diffInfo.binarySizes.previousSize;
    const currentSize = props.diffInfo.binarySizes.currentSize;
    if (props.diffInfo.areBinaryEqual) {
        return (
            <div>
                Images are the same.
                <br />
                Size: {currentSize}
            </div>
        );
    }

    let previousImage = (<div></div>);
    if (previousSize > 0) {
        previousImage = (
            <div>
                <img src={props.diffInfo.previousFileUrl}/>
            </div>
        );
    }

    let currentImage = (<div></div>);
    if (currentSize > 0) {
        currentImage = (
            <div>
                <img src={props.diffInfo.currentFileUrl}/>
            </div>
        );
    }

    return (
        <div>
            <div>
                Images are different.
                <br />
                Previous size: {previousSize} bytes.
                <br />
                Current size: {currentSize} bytes.
            </div>
            {previousImage}
            {currentImage}
        </div>
    );
};

export default imageDiffView;
