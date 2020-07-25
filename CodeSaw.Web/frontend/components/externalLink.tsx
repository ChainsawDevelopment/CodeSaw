import * as React from 'react';
import Icon from '@ui/elements/Icon';
import { IconSizeProp } from '@ui/elements/Icon/Icon';

const ExternalLink = (props: { url: string; size?: IconSizeProp }): JSX.Element => (
    <a href={props.url} target="_blank" rel="noreferrer">
        <Icon name="external alternate" size={props.size || 'tiny'} color="teal" />
    </a>
);

export default ExternalLink;
