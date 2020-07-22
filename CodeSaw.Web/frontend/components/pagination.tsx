import * as React from 'react';
import UIPagination, { PaginationProps } from '@ui/addons/Pagination';
import { PageInfo } from '../api/reviewer';

interface Props {
    page: PageInfo;
    onPageChange?(page: number): void;
}

const Pagination = (props: Props): JSX.Element => {
    const { page } = props;

    const onChange = (e: React.SyntheticEvent<any>, data: PaginationProps) => {
        if (props.onPageChange) props.onPageChange(parseInt(data.activePage.toString()));
    };

    return <UIPagination activePage={page.page} totalPages={page.totalPages} onPageChange={onChange} />;
};

export default Pagination;
