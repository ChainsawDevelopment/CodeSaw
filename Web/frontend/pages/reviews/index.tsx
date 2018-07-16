import * as React from "react";
import { Link } from 'react-router-dom';
import List from '@ui/elements/List';
import Image from '@ui/elements/Image';
import Segment from '@ui/elements/Segment';
import { connect, Dispatch } from "react-redux";
import { RootState } from "../../rootState";
import { loadReviews } from "./state";
import { OnMount } from "../../components/OnMount";
import { Review } from "../../api/reviewer";
import "./reviews.less";

const ReviewItem = (props: {review: Review}) => {
    return (
        <List.Item className="review-item">
            <Image avatar src={props.review.author.avatarUrl} />
            <List.Content>
                <List.Header>
                    <span className="project">{props.review.project}</span>
                    <span className="review-title"><Link to={`/project/${props.review.reviewId.projectId}/review/${props.review.reviewId.reviewId}`}>{props.review.title}</Link></span>
                </List.Header>
                <List.Description>{props.review.changesCount} changes by {props.review.author.givenName}</List.Description>
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
        <Segment>
            <List divided relaxed>
                {props.list.map(r => (<ReviewItem key={`${r.reviewId.projectId}/${r.reviewId.reviewId}`} review={r} />))}
            </List>
        </Segment>
    );
};

const ProjectReviewsList = (props: Props) => {
    const grouped = props.list.reduce((map: Map<string, Review[]>, item: Review) => {
        const group = map.get(item.project);
        if (!group) {
            map.set(item.project, [item]);
        } else {
            group.push(item);
        }

        return map;
    }, new Map<string, Review[]>());

    const reviewLists = [];
    for (const key of grouped.keys()) {
        const items = grouped.get(key);
        reviewLists.push(<ReviewsList key={key} list={items} loadReviews={props.loadReviews} />);
    }

    return (
        <>
            {reviewLists}
        </>
    );
};

const Reviews = (props: Props) => (
    <>
        <h2>Reviews</h2>
        <OnMount onMount={props.loadReviews}/>
        <ProjectReviewsList {...props} />
    </>
);


export default connect(
    (state: RootState): StateProps => ({
        list: state.reviews.reviews
    }),
    (dispatch: Dispatch): DispatchProps => ({
        loadReviews: () => dispatch(loadReviews())
    })
)(Reviews);
