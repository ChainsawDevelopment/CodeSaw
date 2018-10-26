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
    orderBy: 'created_at' | 'updated_at';
    sort: 'asc' | 'desc';
    page: Paged<Review>;
}

interface DispatchProps {
    loadReviews(args: ReviewSearchArgs): void;
}

type Props = StateProps & DispatchProps;

const initialSearchArgs: ReviewSearchArgs = {
    orderBy: 'updated_at',
    sort: 'desc',
    nameFilter: '',
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
        orderBy: state.reviews.orderBy,
        sort: state.reviews.sort,
        page: state.reviews.reviews
    }),
    (dispatch: Dispatch): DispatchProps => ({
        loadReviews: (args) => dispatch(loadReviews(args))
    })
)(Reviews);
