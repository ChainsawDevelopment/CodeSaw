import Menu from "@ui/collections/Menu";
import Icon from '@ui/elements/Icon';
import * as React from "react";
import { KeyboardHelp } from "./KeyboardHelp";
import { HotKeys } from "CodeSaw.Web/frontend/components/HotKeys";
import { Link } from "react-router-dom";

export default class TopMenu extends React.Component<{}, {isOpen:boolean}> {
    constructor(props) {
        super(props);
        this.state = {isOpen: false};
    }

    private handleClose = () => this.setState({isOpen: false});
    private handleOpen = () => this.setState({isOpen: true});

    render() : JSX.Element {
        const helpHotKeys = {
            '?': () => this.setState({isOpen: true})
        };
    
        return <Menu.Item>
                <HotKeys config={helpHotKeys} />
                <KeyboardHelp isOpen={this.state.isOpen} handleClose={this.handleClose} />
                <Link to={'#'} onClick={this.handleOpen} ><Icon name="help" circular /></Link>
            </Menu.Item>
    }
}

