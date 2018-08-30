import * as React from "react";
import "./branchName.less";

const Branch = (props: { name:string }): JSX.Element => (<span className='branch-name'>{props.name}</span>);

export default Branch;