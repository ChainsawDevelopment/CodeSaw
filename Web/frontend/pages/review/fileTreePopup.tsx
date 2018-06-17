import Popup from '@ui/modules/Popup';
import Button from '@ui/elements/Button';

import * as React from "react";
import { RevisionRangeInfo } from "../../api/reviewer";
import { FileInfo } from "./state";
import ChangedFileTree from './changedFileTree';
import { PathPair } from '../../pathPair';

interface State {
    opened: boolean;
}

type SelectFileForViewHandler = (path: PathPair) => void;

export interface Props {
    paths: PathPair[];
    selected:PathPair;
    reviewedFiles: PathPair[];
    onSelect(path: PathPair): void;
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
                />
            </div>
        );

        return (
            <Popup
                open={this.state.opened}
                trigger={<Button color='green' content='Files' />}
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