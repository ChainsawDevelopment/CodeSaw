import * as React from 'react'
import Modal from 'semantic-ui-react/dist/commonjs/modules/Modal'
import Input from '@ui/elements/Input';

interface Props {
    isOpen: boolean
    handleClose(line: string): void;
}

interface State {
    line: string;
}

export class SelectLineNumberModal extends React.Component<Props, State> {
    constructor(props) {
        super(props);

        this.state = {
            line: ''
        };
    }

    private handleInput = (e, data) => {
        this.setState({line: data.value});
    }

    private handleClose = () => {
        this.props.handleClose(this.state.line);
    }

    private handleRef = (inputRef: Input) => {
        if (inputRef) {
            inputRef.focus();
        }
    }

    render(): JSX.Element {
        return <Modal
                open={this.props.isOpen}
                onClose={this.handleClose}
                className={"select-line"}
                size={"mini"}
            >
                <Modal.Header>Select Line</Modal.Header>
                <Modal.Content>
                    <Modal.Description>
                        <Input 
                            ref={this.handleRef} 
                            placeholder="Line number..."
                            icon='search'
                            onChange={this.handleInput}
                            onKeyPress={e => (e.key == "Enter") && this.handleClose()} />
                    </Modal.Description>
                </Modal.Content>
            </Modal>
    }
}