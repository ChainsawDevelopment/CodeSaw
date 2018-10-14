import * as React from "react";
import { Link } from "react-router-dom";
import { ReviewId } from "../../api/reviewer";
import * as PathPairs from "../../pathPair";

export const createLinkToFile = (reviewId: ReviewId, file: PathPairs.PathPair) : string => (
    `/project/${reviewId.projectId}/review/${reviewId.reviewId}/${encodeURIComponent(file.newPath)}`);

export const FileLink = (props: {reviewId: ReviewId, path: PathPairs.PathPair, children?: any}) => {
    return <Link 
        to={createLinkToFile(props.reviewId, props.path)}>
        {props.children || props.path.newPath}
    </Link>
}