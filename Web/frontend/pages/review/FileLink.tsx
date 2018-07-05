import * as React from "react";
import { Link } from "react-router-dom";
import { ReviewId } from "../../api/reviewer";
import * as PathPairs from "../../pathPair";

export const FileLink = (props: {reviewId: ReviewId, path: PathPairs.PathPair, children?: any}) => {
    return <Link 
        to={`/project/${props.reviewId.projectId}/review/${props.reviewId.reviewId}/${encodeURIComponent(props.path.newPath)}`}>
        {props.children || props.path.newPath}
    </Link>
}