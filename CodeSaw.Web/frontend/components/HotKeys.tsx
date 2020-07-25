import * as React from 'react';

interface Props {
    config: any;
}

export class HotKeys extends React.Component<Props> {
    keyDownListener: (event: any) => void;

    public componentDidMount(): void {
        this.keyDownListener = (event: any): void => {
            const inputTags = ['input', 'textarea'];

            const activeElement = document.activeElement;
            if (activeElement && inputTags.indexOf(activeElement.tagName.toLowerCase()) !== -1) {
                return;
            }

            const hotKeyName = this.getHotKeyName(event);

            if (this.props.config[hotKeyName]) {
                this.props.config[hotKeyName](event);
                event.preventDefault();
            }
        };

        document.addEventListener('keydown', this.keyDownListener);
    }

    public componentWillUnmount(): void {
        document.removeEventListener('keydown', this.keyDownListener);
    }

    private getHotKeyName(event) {
        if (event.ctrlKey) {
            return 'ctrl+' + event.key;
        }

        return event.key;
    }

    render(): JSX.Element {
        return null;
    }
}
