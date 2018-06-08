import Popup from 'semantic-ui-react/dist/commonjs/modules/Popup';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';

import * as React from "react";
import { RevisionRangeInfo, PathPair, emptyPathPair } from "../../api/reviewer";
import { FileInfo } from "./state";
import ChangedFileTree from './changedFileTree';

interface State {
    opened: boolean;
}

type SelectFileForViewHandler = (path: PathPair) => void;

export interface Props {
    paths: PathPair[];
    selected:PathPair;
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
            <ChangedFileTree
                paths={this.props.paths}
                selected={this.props.selected}
                onSelect={this._onSelect}
            />
        );

        return (
            <Popup
                open={this.state.opened}
                trigger={<Button color='green' content='Files' />}
                content={filesSelector}
                onOpen={this._onOpen}
                onClose={this._onClose}
                on='click'
                position='top right'
                wide='very'
            />
        );
    }
}