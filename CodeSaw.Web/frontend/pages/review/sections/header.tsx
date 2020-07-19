import { connect } from "react-redux";
import * as React from "react";
import Grid from '@ui/collections/Grid';
import ExternalLink from "@src/components/externalLink";
import Dropdown from "@ui/modules/Dropdown";
import Button from '@ui/elements/Button';
import Input from '@ui/elements/Input';
import vscodeLogo from '@src/assets/vscode.png'
import UserInfo from "@src/components/UserInfo";
import { UserState, RootState } from "@src/rootState";
import { ReviewInfo } from "@api/reviewer";
import { Dispatch } from "redux";
import { saveVSCodeWorkspace } from "../state";

const VSCodeWorkspaceEditor = (props: { path: string; save: (newPath: string) => void; }): JSX.Element => {
    const [value, setValue] = React.useState(props.path);

    return <Input
        fluid
        action={<Button
            primary
            icon='save'
            onClick={() => props.save(value)}
        />
        }
        placeholder='VS Code workspace path'
        onChange={(e, data) => setValue(data.value)}
        defaultValue={value}
    />;
};

interface StateProps {
    currentReview: ReviewInfo;
    author: UserState;
    vsCodeWorkspace: string;
}

interface DispatchProps {
    saveVSCodeWorkspace(vsCodeWorkspace: string): void;
}

type Props = StateProps & DispatchProps;

const Header = (props: Props): JSX.Element => {
    const [showVsCodeWorkspaceEditor, setShowVsCodeWorkspaceEditor] = React.useState(false);

    return <Grid.Row>
        <Grid.Column className={"header"}>
            <Grid.Row>
                <h1>Review {props.currentReview.title} <ExternalLink url={props.currentReview.webUrl} /></h1>
                <h3 className="branch-header">{props.currentReview.projectPath}
                    {props.currentReview.projectPath ?
                        <Dropdown floating inline icon='setting'>
                            <Dropdown.Menu>
                                <Dropdown.Item onClick={() => setShowVsCodeWorkspaceEditor(true)}>
                                    <img src={vscodeLogo} /> Set VS Code workspace for {props.currentReview.projectPath}</Dropdown.Item>
                            </Dropdown.Menu>
                        </Dropdown>
                        : null}
                </h3>

                {showVsCodeWorkspaceEditor ?
                    <VSCodeWorkspaceEditor path={props.vsCodeWorkspace} save={(vsCodeWorkspace: string) => {
                        props.saveVSCodeWorkspace(vsCodeWorkspace);
                        setShowVsCodeWorkspaceEditor(false);
                    }} />

                    : null}

            </Grid.Row>
            <Grid.Row>
                <UserInfo
                    username={props.author.username}
                    name={props.author.name}
                    avatarUrl={props.author.avatarUrl}
                />
            </Grid.Row>
        </Grid.Column>
    </Grid.Row>;
}

export default connect(
    (state: RootState): StateProps => ({
        author: state.review.currentReview.author,
        currentReview: state.review.currentReview,
        vsCodeWorkspace: state.review.vsCodeWorkspace
    }),
    (dispatch: Dispatch) => ({
        saveVSCodeWorkspace: (vsCodeWorkspace: string) => dispatch(saveVSCodeWorkspace({ vsCodeWorkspace }))
    })
)(Header);