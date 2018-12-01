import * as React from 'react'
import Modal from 'semantic-ui-react/dist/commonjs/modules/Modal'
import "./keyboardHelp.less"

interface Props {
    isOpen: boolean
    handleClose(): void;
}

export class KeyboardHelp extends React.Component<Props> {

    render(): JSX.Element {
        return <div className={"keyboard-help"}>
            <Modal
                open={this.props.isOpen}
                onClose={this.props.handleClose}
                className={"keyboard-help"}
            >
                <Modal.Header>Keyboard Shortcuts</Modal.Header>
                <Modal.Content>
                    <Modal.Description>
                        <ul>
                            <li><code>?</code> - this help</li>
                            <li><code>[ ]</code> - previous/next unreviewed file</li>
                            <li><code>{'{ }'}</code> - previous/next file with unanswered comments that need resolution</li>
                            <li><code>ctrl+p</code> - go to file by name</li>
                            <li><code>ctrl+g</code> - comment line</li>
                            <li><code>y</code> - mark file as reviewed/unreviewed</li>
                            <li><code>ctrl+enter</code> - publish current review</li>
                        </ul>
                    </Modal.Description>
                </Modal.Content>
            </Modal>
        </div >
    }
}