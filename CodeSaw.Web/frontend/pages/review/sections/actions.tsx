import { connect } from "react-redux";
import * as React from "react";
import Menu from '@ui/collections/Menu';
import { PublishButton } from "../PublishButton";
import Button from '@ui/elements/Button';
import Checkbox from '@ui/modules/Checkbox';
import { Dispatch } from "redux";
import { markEmptyFilesAsReviewed } from "../state";

interface OwnProps {
    onHideReviewedChange(hide: boolean): void;
}

interface DispatchProps {
    markNonEmptyAsViewed(): void;
}

type Props = OwnProps & DispatchProps;

const Actions = (props: Props): JSX.Element => {
    return <Menu secondary id="summary-menu">
        <Menu.Menu position='right'>
            <Menu.Item>
                <PublishButton />&nbsp;
                <Button onClick={props.markNonEmptyAsViewed}>Mark Unchanged Files</Button>&nbsp;
                <Checkbox toggle label="Hide reviewed" onChange={(e, data) => props.onHideReviewedChange(data.checked)} />&nbsp;
            </Menu.Item>
        </Menu.Menu>
    </Menu>;
}

export default connect(
    () => ({}),
    (dispatch: Dispatch): DispatchProps => ({
        markNonEmptyAsViewed: () => dispatch(markEmptyFilesAsReviewed({}))
    })
)(Actions);