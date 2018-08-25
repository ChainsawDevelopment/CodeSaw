import * as React from "react";
import { Link } from 'react-router-dom';
import List from '@ui/elements/List';
import Image from '@ui/elements/Image';
import Segment from '@ui/elements/Segment';
import { Review } from "../../api/reviewer";

const ReviewItem = (props: {review: Review}) => {
    return (
        <List.Item className="review-item">
            <Image avatar src={props.review.author.avatarUrl} />
            <List.Content>
                <List.Header>
                    <span className="project">{props.review.project}</span>
                    <span className="review-title"><Link to={`/project/${props.review.reviewId.projectId}/review/${props.review.reviewId.reviewId}`}>{props.review.title}</Link></span>
                </List.Header>
                <List.Description>
                    {props.review.changesCount} changes by {props.review.author.givenName}<br />
                    Link: <a href={props.review.webUrl}>{props.review.webUrl}</a>
                </List.Description>
            </List.Content>
        </List.Item>
    );
};

const ReviewsList = (props: {list: Review[]}) => {
    return (
        <Segment>
            <List divided relaxed>
                {props.list.map(r => (<ReviewItem key={`${r.reviewId.projectId}/${r.reviewId.reviewId}`} review={r} />))}
            </List>
        </Segment>
    );
};

const ProjectReviewsList = (props: {list: Review[]}) => {
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
        reviewLists.push(<ReviewsList key={key} list={items} />);
    }

    return (
        <>
            {reviewLists}
        </>
    );
};

export default ProjectReviewsList;