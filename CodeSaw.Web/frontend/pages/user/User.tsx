import * as React from "react";
import { connect } from "react-redux";
import { RootState, UserState } from "../../rootState";
import { Dispatch } from "redux";
import { OnMount } from "../../components/OnMount";
import { loadCurrentUser } from "./state";

export interface UserProps {
    user: UserState;
}

interface DispatchProps {
    loadCurrentUser: () => void;
}

export const User = (props: UserProps) => {
    return (
        <>
            <span className="user">{props.user.name || "Unknown"}</span>
        </>
    );
}

const currentUser = (props: UserProps & DispatchProps) => {
       return (
        <>
            <OnMount onMount={() => props.loadCurrentUser()} />
            <User user={props.user} />
        </>
    );
}

export default connect(
    (state: RootState): UserProps => ({
        user: state.currentUser
    }),
    (dispatch: Dispatch): DispatchProps => ({
        loadCurrentUser: () => dispatch(loadCurrentUser())
    })
)(currentUser);

