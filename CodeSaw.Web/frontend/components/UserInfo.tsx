import * as React from "react";
import "./userInfo.less";

const userInfo = (props: { username: string, name: string, avatarUrl: string }): JSX.Element => (
    <div className='user-info'>
        <img src={props.avatarUrl} alt={props.name} /> 
        <span>Created by: {props.name}</span>
    </div>
);

export default userInfo;