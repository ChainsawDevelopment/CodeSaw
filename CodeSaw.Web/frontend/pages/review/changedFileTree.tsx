import * as React from "react";
import List from '@ui/elements/List';
import ReviewMark from './reviewMark';
import * as PathPairs from "../../pathPair";
import { FileLink } from "./FileLink";
import { ReviewId } from "../../api/reviewer";

const FileItem = (props: { path: PathPairs.PathPair, reviewId: ReviewId, isSelected: boolean, isReviewed: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<span className='selected-file'>{props.path.newPath}</span>)
    } else {
        header = (<FileLink reviewId={props.reviewId} path={props.path} />);
    }

    return (
        <List.Item className="file-tree-item">
            <List.Content>
                <ReviewMark reviewed={props.isReviewed} size='small'/>
                <span>{header}</span>
            </List.Content>
        </List.Item>
    );
}

const changedFileTree = (props: { paths: PathPairs.List, reviewedFiles: PathPairs.List; selected:PathPairs.PathPair, onSelect: (path: PathPairs.PathPair) => void, reviewId: ReviewId }) => {
    const items = props.paths.map(p => (
        <FileItem 
            key={p.newPath} 
            path={p} 
            isSelected={p.newPath == props.selected.newPath}
            isReviewed={PathPairs.contains(props.reviewedFiles, p)}
            onclick={() => props.onSelect(p)}
            reviewId={props.reviewId}
        />
    ));

    return (
        <List className="file-tree">
            {items}
        </List>
    );
};

export default changedFileTree;