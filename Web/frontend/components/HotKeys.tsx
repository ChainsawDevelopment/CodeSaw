import * as React from "react";

interface Props {
    config: any;
}

export class HotKeys extends React.Component<Props> {
    keyDownListener: (event: any) => void;

    public componentDidMount(): void {
        this.keyDownListener = (event: any) : void =>  {
            const hotKeyName = this.getHotKeyName(event);
    
            if (this.props.config[hotKeyName]) {
                this.props.config[hotKeyName](event);
                event.preventDefault();
            }
        }

        document.addEventListener('keydown', this.keyDownListener);
        console.log("HotKeys handler mounted", this.props.config);
    }

    public componentWillUnmount(): void {
        document.removeEventListener('keydown', this.keyDownListener);
        console.log("HotKeys handler unmounted", this.props.config);
    }

    private getHotKeyName(event) {
        if (event.ctrlKey) {
            return "ctrl+"+event.key;
        }
    
        return event.key;
    }

    render() {
        return null;
    }
}