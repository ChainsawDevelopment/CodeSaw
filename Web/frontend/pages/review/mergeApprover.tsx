import * as React from "react";

import "./mergeApprover.less";
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Checkbox from 'semantic-ui-react/dist/commonjs/modules/Checkbox';
import { ReviewId, ReviewInfoState } from "../../api/reviewer";

interface Props {
    reviewId: ReviewId,
    reviewState: ReviewInfoState,
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
}

const OpenedMergeRequest = (props: Props): JSX.Element => {
    const onButtonClick = (): void => {
        console.log('merge!');
        props.mergePullRequest(props.reviewId, false, null);
    }

    var id = props.reviewId;

    return (
        <div className="merge-approver">
            <Button
                id={"merge-button"} 
                onClick={(e, v) => onButtonClick()}
                color='green'>Merge</Button>
            <Checkbox 
                label= { "Remove source branch" } 
            />
        </div>
    );
};

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