import * as React from 'react';
import Button from '@ui/elements/Button';

import { ReviewState, publishReview, FileReviewStatusChange } from './state';
import { RootState } from '../../rootState';
import { connect } from 'react-redux';
import { FileId } from '@api/reviewer';
import { Dispatch } from 'redux';

export namespace PublishButton {
    export interface OwnProps {}

    export interface StateProps {
        review: ReviewState;
    }

    export interface DispatchProps {
        publishReview(fileToLoad: FileId): void;
    }

    export type Props = OwnProps & StateProps & DispatchProps;
}

const PublishButtonView = (props: PublishButton.Props): JSX.Element => {
    const countChanges = (changeStatus: FileReviewStatusChange[]) => changeStatus.length;

    const unpublishedItemsCount =
        props.review.unpublishedFileDiscussions.length +
        props.review.unpublishedReplies.length +
        props.review.unpublishedResolvedDiscussions.length +
        props.review.unpublishedReviewDiscussions.length +
        countChanges(props.review.unpublishedReviewedFiles) +
        countChanges(props.review.unpublishedUnreviewedFiles);

    const publishAndLoad = props.review.selectedFile
        ? () => props.publishReview(props.review.selectedFile.fileId)
        : () => props.publishReview(undefined);

    return (
        <Button disabled={unpublishedItemsCount === 0} color="teal" onClick={publishAndLoad}>
            Publish changes {unpublishedItemsCount > 0 && <span className={'count'}>({unpublishedItemsCount})</span>}
        </Button>
    );
};

const mapStateToProps = (state: RootState): PublishButton.StateProps => ({
    review: state.review,
});

const mapDispatchToProps = (dispatch: Dispatch, ownProps: PublishButton.OwnProps): PublishButton.DispatchProps => ({
    publishReview: (fileToLoad: string) => dispatch(publishReview({ fileToLoad })),
});

export const PublishButton = connect(mapStateToProps, mapDispatchToProps)(PublishButtonView);
