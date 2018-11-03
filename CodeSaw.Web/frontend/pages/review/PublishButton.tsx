import * as React from "react";
import Button from '@ui/elements/Button';

import { ReviewState, publishReview, FileReviewStatusChange } from "./state";
import { RootState } from "../../rootState";
import { Dispatch, connect } from "react-redux";

export namespace PublishButton {
    export interface OwnProps {
    }

    export interface StateProps {
        review: ReviewState
    }

    export interface DispatchProps {
        publishReview(fileToLoad: string): void;
    }

    export type Props = OwnProps & StateProps & DispatchProps;
}

const PublishButtonView = (props: PublishButton.Props) : JSX.Element => {
    const countChanges = (changeStatus: FileReviewStatusChange) => Object.keys(changeStatus)
            .map(key => changeStatus[key])
            .reduce((a,b) => a.concat(b), [])
            .length;

    const unpublishedItemsCount = 
        props.review.unpublishedFileDiscussions.length +
        props.review.unpublishedReplies.length +
        props.review.unpublishedResolvedDiscussions.length +
        props.review.unpublishedReviewDiscussions.length + 
        countChanges(props.review.unpublishedReviewedFiles) +
        countChanges(props.review.unpublishedUnreviewedFiles);

    const publishAndLoad = props.review.selectedFile ?
        () => props.publishReview(props.review.selectedFile.path.newPath)
        : () => props.publishReview(undefined);
    
    return <Button 
        disabled={unpublishedItemsCount === 0} 
        positive 
        onClick={publishAndLoad}>
            Publish Changes {unpublishedItemsCount > 0 && <span className={"count"}>({unpublishedItemsCount})</span>}
        </Button>
}

const mapStateToProps = (state: RootState): PublishButton.StateProps => ({
    review: state.review
});

const mapDispatchToProps = (dispatch: Dispatch, ownProps: PublishButton.OwnProps): PublishButton.DispatchProps => ({
    publishReview: (fileToLoad: string) => dispatch(publishReview({ fileToLoad })),
})

export const PublishButton = connect(
    mapStateToProps,
    mapDispatchToProps
)(PublishButtonView);