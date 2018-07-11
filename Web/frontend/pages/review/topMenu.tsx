import Menu from "@ui/collections/Menu";
import * as React from "react";
import { connect } from "react-redux";
import { publishReview, createGitLabLink } from "./state";
import { ReviewId } from "../../api/reviewer";
import { RootState } from "../../rootState";

interface StateProps {
    reviewId: ReviewId;
}

type Props = StateProps;

const topMenu = (props: Props) => {

    return (
        <span></span>
    );
}

export default connect(
    (state: RootState): StateProps => ({
        reviewId: state.review.currentReview.reviewId
    })
)(topMenu);