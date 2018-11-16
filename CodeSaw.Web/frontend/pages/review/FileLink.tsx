import * as React from "react";
import { Link } from "react-router-dom";
import { ReviewId, FileId } from "../../api/reviewer";
import FileName from './fileName';

export const createLinkToFile = (reviewId: ReviewId, fileId: FileId) : string => (
    `/project/${reviewId.projectId}/review/${reviewId.reviewId}/${fileId}`);

export const FileLink = (props: {reviewId: ReviewId, fileId: FileId, children?: any}) => {
    return <Link 
        to={createLinkToFile(props.reviewId, props.fileId)}>
        {props.children || <FileName fileId={props.fileId} />}
    </Link>
}