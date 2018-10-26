import * as React from "react";
import CurrentReviewMode from './currentReviewMode';

class ReviewModeCore extends React.PureComponent<{ mode: 'reviewer' | 'author' }> {
    render() {
        const componentType = this.props.mode == 'reviewer' ? ReviewModeCore.Reviewer : ReviewModeCore.Author;

        const b = React.Children.toArray(this.props.children)
            .filter(s => (s as any).type == componentType);

        return <>{b}</>;
    }

    static readonly Author = (props: { children?: React.ReactNode }): JSX.Element => {
        return <>{props.children}</>;
    }

    static readonly Reviewer = (props: { children?: React.ReactNode }): JSX.Element => {
        return <>{props.children}</>;
    }
}

class ReviewMode extends React.Component {
    render() {
        return (
            <CurrentReviewMode.Consumer>
            {mode => <ReviewModeCore mode={mode} children={this.props.children} />}
            </CurrentReviewMode.Consumer>
        );
    }

    static readonly Author = ReviewModeCore.Author;
    static readonly Reviewer = ReviewModeCore.Reviewer;
}

export default ReviewMode;