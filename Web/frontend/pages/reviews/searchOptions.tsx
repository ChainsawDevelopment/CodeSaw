import * as React from "react";
import { ReviewSearchArgs, PageInfo } from "../../api/reviewer";
import Pagination from "../../components/pagination";

interface Props {
    currentPage: PageInfo;
    initialArgs: ReviewSearchArgs;
    loadResults(args: ReviewSearchArgs): void;
}

const SearchOptions = (props: Props): JSX.Element => {
    const onPageChange = (page: number) => props.loadResults({
        page: page,
        state: 'opened'
    })

    return (
        <Pagination page={props.currentPage} onPageChange={onPageChange} />
    );
}

export default SearchOptions;