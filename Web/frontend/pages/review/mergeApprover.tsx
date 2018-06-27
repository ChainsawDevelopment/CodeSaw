import * as React from "react";

import "./mergeApprover.less";
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Checkbox, { CheckboxProps } from 'semantic-ui-react/dist/commonjs/modules/Checkbox';
import { ReviewId, ReviewInfoState } from "../../api/reviewer";

interface StatelessProps {
    reviewId: ReviewId,
    reviewState: ReviewInfoState,
    shouldRemoveBranch: boolean;
    setRemoveBranchFlag(remove: boolean): void;
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
}

const OpenedMergeRequestStateless = (props: StatelessProps): JSX.Element => {
    const onButtonClick = (): void => {
        props.mergePullRequest(props.reviewId, props.shouldRemoveBranch, null);
    }

    const handleRemoveBranchCheckbox = (e: React.SyntheticEvent<HTMLInputElement>, d: CheckboxProps) => {
        props.setRemoveBranchFlag(d.checked);
    };

    return (
        <div className="merge-approver">
            <Button
                id={"merge-button"} 
                onClick={(e, v) => onButtonClick()}
                color='green'>Merge</Button>
            <Checkbox 
                label= { "Remove source branch" } 
                checked={props.shouldRemoveBranch}
                onChange={handleRemoveBranchCheckbox}
            />
        </div>
    );
};

interface Props {
    reviewId: ReviewId,
    reviewState: ReviewInfoState,
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
}


class OpenedMergeRequest extends React.Component<Props, { removeSourceBranch: boolean }> {
    constructor(props) {
        super(props)
        this.state = { removeSourceBranch: true };
    }

    private _setFlag = (removeSourceBranch: boolean) => {
        this.setState({removeSourceBranch});
    }

    render() {
        return (<OpenedMergeRequestStateless
            {...this.props}
            shouldRemoveBranch={this.state.removeSourceBranch}
            setRemoveBranchFlag={this._setFlag}
        />);
    }
}

const NotMergableRequest = (props: Props): JSX.Element => 
     (<div>Review is <span className="review-state">{props.reviewState}</span></div>)

const mergeApprover = (props: Props): JSX.Element => {
    if (props.reviewState == "opened") {
        return (<OpenedMergeRequest {...props}/>)
    } else {
        return (<NotMergableRequest {...props} />)
    }
};

export default mergeApprover;