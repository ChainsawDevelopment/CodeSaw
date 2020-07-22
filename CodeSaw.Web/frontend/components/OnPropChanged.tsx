import * as React from 'react';

export class OnPropChanged extends React.Component<any> {
    public componentDidUpdate(prevProps: Readonly<any>): void {
        const changedKeys = Object.keys(this.props).filter(
            (propName) => propName != 'onPropChanged' && prevProps[propName] != this.props[propName],
        );
        if (changedKeys && changedKeys.length > 0 && this.props.onPropChanged) {
            this.props.onPropChanged(changedKeys);
        }
    }

    render(): JSX.Element {
        return null;
    }
}
