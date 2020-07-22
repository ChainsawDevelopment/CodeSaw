import Popup from '@ui/modules/Popup';
import Button from '@ui/elements/Button';

import * as React from 'react';
import { ReviewId, FileId } from '../../api/reviewer';
import ChangedFileTree from './changedFileTree';
import { PathPair } from '../../pathPair';
import { HotKeys } from 'CodeSaw.Web/frontend/components/HotKeys';
import CurrentReviewMode from './currentReviewMode';

interface State {
    opened: boolean;
}

export interface Props {
    files: { id: FileId; name: PathPair }[];
    selected: FileId;
    reviewedFiles: FileId[];
    onSelect(fileId: FileId): void;
    reviewId: ReviewId;
}

export default class ChangedFileTreePopup extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            opened: false,
        };
    }

    private _onOpen = () => {
        this.setState({ opened: true });
    };

    private _onClose = () => {
        this.setState({ opened: false });
    };

    private _onSelect = (fileId: FileId) => {
        this._onClose();
    };

    render(): any {
        const filesSelector = (
            <div className="file-tree-popup">
                <ChangedFileTree
                    files={this.props.files}
                    selected={this.props.selected}
                    reviewedFiles={this.props.reviewedFiles}
                    onSelect={this._onSelect}
                    reviewId={this.props.reviewId}
                />
            </div>
        );

        const fileSearchHotKeys = {
            'ctrl+p': this._onOpen,
        };

        return (
            <CurrentReviewMode.Consumer>
                {(mode) => (
                    <span>
                        <HotKeys config={fileSearchHotKeys} />

                        <Popup
                            open={this.state.opened}
                            trigger={<Button secondary content="Choose File..." />}
                            content={
                                <CurrentReviewMode.Provider value={mode}>{filesSelector}</CurrentReviewMode.Provider>
                            }
                            onOpen={this._onOpen}
                            onClose={this._onClose}
                            hideOnScroll={false}
                            on="click"
                            position="bottom left"
                            wide="very"
                        />
                    </span>
                )}
            </CurrentReviewMode.Consumer>
        );
    }
}
