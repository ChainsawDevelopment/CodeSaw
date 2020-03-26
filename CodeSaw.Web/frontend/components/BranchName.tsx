import * as React from "react";
import * as Clipboard from "clipboard"
import Icon from "@ui/elements/Icon";
import Popup from "@ui/modules/Popup";

import "./branchName.less";

const Branch = (props: { name: string }): JSX.Element => (<span id={props.name} className='branch-name'>{props.name}
    {props.name ? <Popup content='Copy branch name' trigger={
        <Icon className={"copy-branch-name"} data-clipboard-text={props.name} color='black' name='copy outline' />
    } />
    : null}
    
</span>);

new Clipboard('.copy-branch-name');

export default Branch;