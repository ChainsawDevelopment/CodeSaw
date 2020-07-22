import Icon from '@ui/elements/Icon';
import Button from '@ui/elements/Button';
import * as React from 'react';
import { IconSizeProp, IconProps } from '@ui/elements/Icon/Icon';

interface Props {
    [key: string]: any;
    reviewed: boolean;
    onClick?: (newState: boolean) => void;
}

export default (props: Props): any => {
    const { reviewed, onClick, ...rest } = props;

    const common = {
        ...rest,
        circular: true,
        inverted: true,
    };

    if (onClick) {
        common['link'] = true;
        common['onClick'] = () => onClick(!reviewed);
    }

    let icon: JSX.Element;

    if (!reviewed) {
        icon = <Icon {...common} name="eye slash outline" color="red" />;
    } else {
        icon = <Icon {...common} name="eye" color="green" />;
    }

    return icon;
};
