import * as React from "react";
import { Link } from 'react-router-dom';
import List from '@ui/elements/List';
import Image from '@ui/elements/Image';
import Segment from '@ui/elements/Segment';
import Label from '@ui/elements/Label';
import { Review } from "../../api/reviewer";
import Branch from "../../components/BranchName";
import ExternalLink from "../../components/externalLink";

const ReviewBadges = (props: {review:Review}) => {
    const badges = [];

    const { isCreatedByMe, amIReviewer } = props.review;

    if (isCreatedByMe) {
        badges.push(<Label key='creator' color='orange' content='Creator' size='mini' />);
    }

    if (!isCreatedByMe && amIReviewer) {
        badges.push(<Label key='reviewer' color='red' content='Reviewer' size='mini' />);
    }

    return (<>{badges}</>);
}

const ReviewItem = (props: {review: Review}) => {
    return (
        <List.Item className="review-item">
            <Image avatar src={props.review.author.avatarUrl} />
            <List.Content>
                <List.Header>
                    <span className="project">{props.review.project}</span>
                    <span className="review-title"><Link to={`/project/${props.review.reviewId.projectId}/review/${props.review.reviewId.reviewId}`}>{props.review.title}</Link></span>
                    <ReviewBadges review={props.review} />
                </List.Header>
                <List.Description>
                    <ExternalLink url={props.review.webUrl} size='small' />
                    Created by {props.review.author.name}. <br />
                    <Branch name={props.review.sourceBranch}/> &rarr; <Branch name={props.review.targetBranch} /><br />
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