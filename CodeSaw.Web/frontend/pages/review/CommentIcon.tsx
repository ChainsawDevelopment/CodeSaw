import * as React from 'react';
import Icon from '@ui/elements/Icon';
import { SemanticICONS } from '@ui/generic';
import { CommentState } from '../../api/reviewer';
import { DiscussionState } from './state';

export const CommentIcon = (props: { commentType: CommentState }): JSX.Element => {
    let iconName: SemanticICONS = 'bug';

    switch (props.commentType) {
        case DiscussionState.GoodWork:
            iconName = 'winner';
            break;
        case DiscussionState.NeedsResolution:
            iconName = 'exclamation triangle';
            break;
        case DiscussionState.Resolved:
            iconName = 'check';
            break;
        case DiscussionState.NoActionNeeded:
            iconName = 'comment';
            break;
        case DiscussionState.ResolvePending:
            iconName = 'ellipsis horizontal';
            break;
        default:
            iconName = 'bug';
            break;
    }

    return <Icon name={iconName} />;
};
