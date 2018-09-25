import Popup from '@ui/modules/Popup';
import Button from '@ui/elements/Button';

import * as React from "react";
import { ReviewId } from "../../api/reviewer";
import ChangedFileTree from './changedFileTree';
import { PathPair } from '../../pathPair';

interface State {
    opened: boolean;
}

export interface Props {
    paths: PathPair[];
    selected:PathPair;
    reviewedFiles: PathPair[];
    onSelect(path: PathPair): void;
    reviewId: ReviewId;
}

export default class ChangedFileTreePopup extends React.Component<Props, State> {
    constructor(props) {
        super(props);

        this.state = {
            opened: false
        };
    }

    private _onOpen = () => {
        this.setState({ opened: true });
    }

    private _onClose = () => {
        this.setState({ opened: false });
    }

    private _onSelect = (p: PathPair) => {
        this.props.onSelect(p);
        this.setState({opened: false});
    }

    render() {
        const filesSelector = (
            <div className='file-tree-popup'>
                <ChangedFileTree
                    paths={this.props.paths}
                    selected={this.props.selected}
                    reviewedFiles={this.props.reviewedFiles}
                    onSelect={this._onSelect}
                    reviewId={this.props.reviewId}
                />
            </div>
        );

        return (
            <Popup
                open={this.state.opened}
                trigger={<Button secondary content='Choose File...' />}
                content={filesSelector}
                onOpen={this._onOpen}
                onClose={this._onClose}
                hideOnScroll={true}
                on='click'
                position='bottom left'
                wide='very'
            />
        );
    }
}