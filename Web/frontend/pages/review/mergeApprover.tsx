import * as React from "react";

import "./mergeApprover.less";
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Checkbox from 'semantic-ui-react/dist/commonjs/modules/Checkbox';
import { ReviewId } from "../../api/reviewer";

interface Props {
    reviewId: ReviewId,
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
}

const mergeApprover = (props: Props): JSX.Element => {
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

export default mergeApprover;