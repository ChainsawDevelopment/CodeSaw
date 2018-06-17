import Menu from "@ui/collections/Menu";
import * as React from "react";
import { connect } from "react-redux";
import { publishReview, createGitLabLink } from "./state";
import { ReviewId } from "../../api/reviewer";
import { RootState } from "../../rootState";

interface StateProps {
    reviewId: ReviewId;
}

interface DispatchProps {
    publishReview(): void;
    createGitLabLink(reviewId: ReviewId);
}

type Props = StateProps & DispatchProps;

const topMenu = (props: Props) => {
    const createLink = () => props.createGitLabLink(props.reviewId);

    return (
        <>
            <Menu.Item
                position='right'
                onClick={createLink}>
                Create link in GitLab
            </Menu.Item>
            <Menu.Item
                className='page-menu publish'
                
                onClick={props.publishReview}>
                Publish
            </Menu.Item>
        </>
    );
}

export default connect(
    (state: RootState): StateProps => ({
        reviewId: state.review.currentReview.reviewId
    }),
    (dispatch): DispatchProps => ({
        publishReview: () => dispatch(publishReview({})),
        createGitLabLink: (reviewId) => dispatch(createGitLabLink({ reviewId }))
    })
)(topMenu);