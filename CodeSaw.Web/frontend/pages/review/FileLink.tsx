import * as React from 'react';
import { Link } from 'react-router-dom';
import { ReviewId, FileId } from '../../api/reviewer';
import FileName from './FileName';

export const createLinkToFile = (reviewId: ReviewId, fileId: FileId): string =>
    `/project/${reviewId.projectId}/review/${reviewId.reviewId}/file/${fileId}`;

export const FileLink = (props: {
    reviewId: ReviewId;
    fileId: FileId;
    children?: any;
    onClick?: () => void;
}): JSX.Element => {
    return (
        <Link to={createLinkToFile(props.reviewId, props.fileId)} onClick={props.onClick}>
            {props.children || <FileName fileId={props.fileId} />}
        </Link>
    );
};
