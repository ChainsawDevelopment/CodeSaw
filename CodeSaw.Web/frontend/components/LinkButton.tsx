import * as React from 'react';
import { withRouter, RouteComponentProps } from 'react-router';
import Button from '@ui/elements/Button';

interface Props extends RouteComponentProps<{}> {
    to: string;
    children?: any;
    onClick?: (event: React.MouseEvent<HTMLButtonElement>) => void;
}

const LinkButton = (props: Props) => {
    const {
        history,
        location,
        match,
        staticContext,
        to,
        onClick,
        // ⬆ filtering out props that `button` doesn’t know what to do with.
        ...rest
    } = props;
    return (
        <Button
            {...rest} // `children` is just another prop!
            onClick={(event) => {
                onClick && onClick(event);
                history.push(to);
            }}
        />
    );
};

export default withRouter(LinkButton);
