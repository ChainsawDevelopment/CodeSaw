import * as React from "react";
import LinkButton from "../../components/LinkButton";
import { Link } from 'react-router-dom';
import List from 'semantic-ui-react/dist/commonjs/elements/List';
import Image from 'semantic-ui-react/dist/commonjs/elements/Image';

const testReviews = [
    { id: 12, title: 'Mix human and protomolecule', files: 30 },
    { id: 21, title: 'Recover Tachi', files: 2 },
    { id: 123, title: 'Inspect Venus', files: 45 },
    { id: 321, title: 'Buy remaining books', files: 12 },
    { id: 322, title: 'It reaches out...', files: 12 },
    { id: 323, title: 'Hit Eros with Navoo so it will not collide with Earth' , files: 12 },
];

const ReviewItem = (props: {review: typeof testReviews[0]}) => {
    console.log(props);

    const link = other => (<Link to={`/review/${props.review.id}`} {...other} />);

    return (
        <List.Item>
            <Image avatar src='https://www.gravatar.com/avatar/00000000000000000000000000000000' />
            <List.Content>
                <List.Header as={link}>{props.review.title}</List.Header>
                <List.Description>{props.review.files} changes</List.Description>
            </List.Content>
        </List.Item>
    );
};

const ReviewsList = (props: {reviews: typeof testReviews}) => {
    return (
        <List>
            {props.reviews.map(r => (<ReviewItem key={r.id} review={r} />))}
        </List>
    );
};

const Reviews = () => (
    <div>
        <h1>Reviews</h1>
        <ReviewsList reviews={testReviews} />
    </div>
);


export default Reviews;