import * as React from 'react';
import Message from '@ui/collections/Message';
import Icon from '@ui/elements/Icon';

interface Env {
    local: boolean;
    debug: boolean;
}

const envMessages = (): JSX.Element => {
    const messages: JSX.Element[] = [];

    const env = (window as any).env as Env;

    if (env.local) {
        messages.push(<Message.Item key='local'>You are using <strong>local</strong> version</Message.Item>);
    }

    if (env.debug) {
        messages.push(<Message.Item key='debug'>You are using <strong>debug</strong> version</Message.Item>);
    }

    if (messages.length == 0) {
        return null;
    }

    return (
        <Message icon warning>
            <Icon name='warning sign'/>
            <Message.Content>
                <Message.Header>CodeSaw Development Environment</Message.Header>
                <Message.List>
                    {messages}
                </Message.List>
            </Message.Content>
        </Message>
    );
};

export default envMessages;