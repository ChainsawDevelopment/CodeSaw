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
;

const ReviewItem = (props: {review: Review}) => {
    console.log(props);

    const link = other => (<Link to={`/review/${props.review.id}`} {...other} />);

    return (
        <List.Item>
            <Image avatar src='https://www.gravatar.com/avatar/00000000000000000000000000000000' />
            <List.Content>
                <List.Header as={link}>{props.review.title}</List.Header>
                <List.Description>{props.review.changedFiles} changes</List.Description>
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
            {props.list.map(r => (<ReviewItem key={r.id} review={r} />))}
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
