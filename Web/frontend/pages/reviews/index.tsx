import * as React from "react";
import LinkButton from "../../components/LinkButton";
import { Link } from 'react-router-dom';
import List from 'semantic-ui-react/dist/commonjs/elements/List';
import Image from 'semantic-ui-react/dist/commonjs/elements/Image';
import { connect, Dispatch } from "react-redux";
import { RootState } from "../../rootState";
import { loadReviews } from "./state";
import { OnMount } from "../../components/OnMount";
import { Review } from "../../api/reviewer";
import "./reviews.less";

const ReviewItem = (props: {review: Review}) => {
    return (
        <List.Item className="review-item">
            <Image avatar src='https://www.gravatar.com/avatar/00000000000000000000000000000000' />
            <List.Content>
                <List.Header>
                    <span className="project">{props.review.project}</span>
                    <span className="review-title"><Link to={`/review/${props.review.reviewId}`}>{props.review.title}</Link></span>
                </List.Header>
                <List.Description>{props.review.changesCount} changes by {props.review.author}</List.Description>
            </List.Content>
        </List.Item>
    );
};

interface StateProps {
    list: Review[];
}

interface DispatchProps {
    loadReviews: () => void;
}

type Props = StateProps & DispatchProps;

const ReviewsList = (props: Props) => {
    return (
        <List>
            {props.list.map(r => (<ReviewItem key={r.reviewId} review={r} />))}
        </List>
    );
};


const Reviews = (props: Props) => (
    <div>
        <h1>Reviews</h1>
        <OnMount onMount={props.loadReviews}/>
        <ReviewsList {...props} />
    </div>
);


export default connect(
    (state: RootState): StateProps => ({
        list: state.reviews.reviews
    }),
    (dispatch: Dispatch): DispatchProps => ({
        loadReviews: () => dispatch(loadReviews())
    })    
)(Reviews);
