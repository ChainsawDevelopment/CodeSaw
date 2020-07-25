import { connect } from 'react-redux';
import * as React from 'react';
import Menu from '@ui/collections/Menu';
import { PublishButton } from '../PublishButton';
import Button from '@ui/elements/Button';
import { Dispatch } from 'redux';
import { markEmptyFilesAsReviewed } from '../state';

interface DispatchProps {
    markNonEmptyAsViewed(): void;
}

type Props = DispatchProps;

const Actions = (props: Props): JSX.Element => {
    return (
        <Menu secondary id="summary-menu">
            <Menu.Menu position="right">
                <Menu.Item>
                    <PublishButton />
                    &nbsp;
                    <Button onClick={props.markNonEmptyAsViewed}>Mark unchanged files</Button>&nbsp;
                </Menu.Item>
            </Menu.Menu>
        </Menu>
    );
};

export default connect(
    () => ({}),
    (dispatch: Dispatch): DispatchProps => ({
        markNonEmptyAsViewed: () => dispatch(markEmptyFilesAsReviewed({})),
    }),
)(Actions);
