import List from '@ui/elements/List';
import * as React from 'react';

import { BuildStatus } from '../api/reviewer';
import { SemanticICONS } from '../../../node_modules/semantic-ui-react';

const statusToIcon = {
    unknown: {
        icon: 'question circle' as SemanticICONS,
        color: null,
    },
    success: {
        icon: 'check circle' as SemanticICONS,
        color: 'teal',
    },
    failed: {
        icon: 'times circle' as SemanticICONS,
        color: 'red',
    },
    running: {
        icon: 'play circle' as SemanticICONS,
        color: 'yellow',
    },
    canceled: {
        icon: 'ban' as SemanticICONS,
        color: 'grey',
    },
    pending: {
        icon: 'pause' as SemanticICONS,
        color: 'orange',
    },
};

const StatusItem = (props: { status: BuildStatus }): JSX.Element => {
    const { name, targetUrl, description, status } = props.status;

    const icon = statusToIcon[status] || statusToIcon['unknown'];

    return (
        <List.Item>
            <List.Icon name={icon.icon} color={icon.color} size="large" verticalAlign="middle" />
            <List.Content>
                <List.Header as="a" href={targetUrl}>
                    {name}
                </List.Header>
                <List.Description>{description}</List.Description>
            </List.Content>
        </List.Item>
    );
};

interface Props {
    statuses: BuildStatus[];
}

const BuildStatusesList = (props: Props): JSX.Element => {
    const { statuses } = props;

    return (
        <List divided relaxed>
            {statuses.map((s) => (
                <StatusItem key={s.name} status={s} />
            ))}
        </List>
    );
};

export default BuildStatusesList;
