import * as React from "react";

type Handler = () => void;

interface Props {
    onMount?: Handler;
    onUnmount?: Handler;
}

export class OnMount extends React.Component<Props> {
    public componentDidMount(): void {
        if (this.props.onMount) {
            this.props.onMount();
        }
    }

    public componentWillUnmount(): void {
        if (this.props.onUnmount) {
            this.props.onUnmount();
        }
    }

    render() {
        return null;
    }
}