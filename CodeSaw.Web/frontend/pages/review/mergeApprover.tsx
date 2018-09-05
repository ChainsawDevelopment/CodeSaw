import * as React from "react";

import "./mergeApprover.less";
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Checkbox, { CheckboxProps } from 'semantic-ui-react/dist/commonjs/modules/Checkbox';
import { ReviewId, ReviewInfoState, ReviewMergeStatus } from "../../api/reviewer";
import Icon from "@ui/elements/Icon";
import Branch from "../../components/BranchName";

interface StatelessProps {
    reviewId: ReviewId,
    reviewState: ReviewInfoState,
    shouldRemoveBranch: boolean;
    sourceBranch: string;
    targetBranch: string;
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
                changes from <Branch name={props.sourceBranch} /> to <Branch name={props.targetBranch} /> and 
            &nbsp;<Checkbox 
                label= { "remove source branch" } 
                checked={props.shouldRemoveBranch}
                onChange={handleRemoveBranchCheckbox}
            />
        </div>
    );
};

interface Props {
    reviewId: ReviewId,
    reviewState: ReviewInfoState,
    mergeStatus: ReviewMergeStatus;
    reviewFinished: boolean;
    sourceBranch: string;
    targetBranch: string;
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
     (<div><Icon color='red' name='times circle' /> Review for changes from <Branch name={props.sourceBranch} /> to <Branch name={props.targetBranch} />  is <span className="review-state">{props.reviewState}</span> but cannot be merged</div>)

const AlreadyMerged = (props: Props): JSX.Element => 
    (<div><Icon color='black' name='flag checkered' /> Review for changes from <Branch name={props.sourceBranch} /> to <Branch name={props.targetBranch} /> is already <span className="review-state">merged</span></div>);

const Closed = (props: Props): JSX.Element => 
    (<div><Icon color='black' name='trash alternate' /> Review for changes from <Branch name={props.sourceBranch} /> to <Branch name={props.targetBranch} /> is <span className="review-state">closed</span>. Changes were discarded.</div>);

const ReviewNotFinished = (props: Props): JSX.Element => 
    (<div><Icon color='black' name='hourglass half' /> Review for changes from <Branch name={props.sourceBranch} /> to <Branch name={props.targetBranch} /> is <span className="review-state">in progress</span>.</div>);



const mergeApprover = (props: Props): JSX.Element => {
    if (!props.reviewFinished) {
        return <ReviewNotFinished  {...props} />;
    } else if (props.reviewState == "opened" && props.mergeStatus == 'can_be_merged') {
        return (<OpenedMergeRequest {...props}/>)
    } else if (props.reviewState == "merged") {
        return (<AlreadyMerged {...props}/>)
    } else if (props.reviewState == "closed") {
        return (<Closed {...props}/>)
    } else {
        return (<NotMergableRequest {...props} />)
    }
};

export default mergeApprover;