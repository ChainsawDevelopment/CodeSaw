import * as React from "react";
import { Link } from 'react-router-dom';
import List from '@ui/elements/List';
import Image from '@ui/elements/Image';
import Segment from '@ui/elements/Segment';
import { connect, Dispatch } from "react-redux";
import { RootState } from "../../rootState";
import { loadReviews } from "./state";
import { OnMount } from "../../components/OnMount";
import { Review, PageInfo, Paged, ReviewSearchArgs } from "../../api/reviewer";
import "./reviews.less";
import ProjectReviewsList from "./reviewList";
import Pagination from "../../components/pagination";
import SearchOptions from "./searchOptions";

interface StateProps {
    page: Paged<Review>;
}

interface DispatchProps {
    loadReviews(args: ReviewSearchArgs): void;
}

type Props = StateProps & DispatchProps;

const initialSearchArgs: ReviewSearchArgs = {
    page: 1,
    state: 'opened'
}

const Reviews = (props: Props) => {
    const initialLoad = () => props.loadReviews(initialSearchArgs);

    return (
        <>
            <h2>Reviews</h2>
            <OnMount onMount={initialLoad} />
            <SearchOptions 
                initialArgs={initialSearchArgs}
                loadResults={props.loadReviews} 
                currentPage={props.page} 
            />
            
            <ProjectReviewsList list={props.page.items} />
        </>
    );
}


export default connect(
    (state: RootState): StateProps => ({
        page: state.reviews.reviews
    }),
    (dispatch: Dispatch): DispatchProps => ({
        loadReviews: (args) => dispatch(loadReviews(args))
    })
)(Reviews);
