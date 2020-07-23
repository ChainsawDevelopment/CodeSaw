import * as React from "react";
import List from '@ui/elements/List';
import { buildTree, FolderTreeNode, FileTreeNode, sortTree, shortTree, nestedName } from "@src/treeNode";
import { FileToReview, ReviewId, FileId, FileDiscussion } from "@api/reviewer";
import { connect } from "react-redux";
import { RootState } from "@src/rootState";
import { FileLink } from "../FileLink";
import { DiscussionState, ReviewState } from "../state";
import Label from '@ui/elements/Label';
import Icon from '@ui/elements/Icon';
import * as classNames from "classnames";
import { SemanticCOLORS, SemanticICONS } from "@ui/generic";
import Popup from "@ui/modules/Popup";

const style = require('./ReviewFilesTree.less');

interface FileStats {
    newGoodWork: number;
    newNeedsResolution: number;
    newNoActionNeeded: number;
    goodWork: number;
    needsResolution: number;
    noActionNeeded: number;
    resolved: number;
}

const emptyFileStats: FileStats = {
    newGoodWork: 0,
    newNeedsResolution: 0,
    newNoActionNeeded: 0,
    goodWork: 0,
    needsResolution: 0,
    noActionNeeded: 0,
    resolved: 0
}

interface NodeProps {
    reviewId: ReviewId;
    selectedFile: FileId;
    reviewedFiles: FileId[];
    fileStats: { [fileId: string]: FileStats };
}

interface FileNodeProps extends NodeProps {
    file: FileTreeNode<FileToReview>;
}

const NodeFileLink = (props: FileNodeProps) => {
    if(props.selectedFile !=  props.file.value.fileId) {
        return <FileLink reviewId={props.reviewId} fileId={props.file.value.fileId}>{props.file.name}</FileLink>;
    } else {
        return <>{props.file.name}</>;
    }
}

const getColor = (newElementCount): SemanticCOLORS => {
    return newElementCount > 0 ? 'teal' : null;
};

const commentIcon = (newCommentsCounter: number, oldCommentsCounter: number, popupLabel: string, key: string, icon: SemanticICONS) => {
    const label =
        <Label color={getColor(newCommentsCounter)} size="mini">
            <Icon name={icon}/>{oldCommentsCounter + newCommentsCounter}
        </Label>;

    return (
        <Popup
            key={key}
            trigger={label}
            content={popupLabel}
            position="bottom center"
            size="tiny"
        />);
}

const NodeFileStats = (props: {stats: FileStats}) => {
    const {stats} = props;
    const description = [];

    if (stats.needsResolution + stats.newNeedsResolution > 0) {
        description.push(commentIcon(stats.newNeedsResolution, stats.needsResolution, "To fix", "discussion-resolution", "exclamation triangle"));
    }
    if (stats.resolved > 0) {
        description.push(commentIcon(0, stats.resolved, "Resolved", "discussion-resolved", "check"));
    }
    if (stats.noActionNeeded + stats.newNoActionNeeded > 0) {
        description.push(commentIcon(stats.newNoActionNeeded, stats.noActionNeeded, "Comment", "discussion-action", "comment"));
    }
    if (stats.goodWork + stats.newGoodWork > 0) {
        description.push(commentIcon(stats.newGoodWork, stats.goodWork, "Good work!", "discussion-good-work", "winner"));
    }

    if(description.length > 0) {
        return <List.Description>{description}</List.Description>
    } else {
        return <></>
    }
}

const ReviewedIcon = (props: { reviewed: boolean }) => {
    const color = props.reviewed ? 'teal' : 'red';

    return <List.Icon name='file alternate' color={color} />;
}

const FileNode = (props: FileNodeProps): JSX.Element => {
    const selected = props.selectedFile == props.file.value.fileId;
    const reviewed = props.reviewedFiles.indexOf(props.file.value.fileId) >= 0;
    const stats = props.fileStats[props.file.value.fileId] || emptyFileStats;

    return <List.Item className={classNames({ file: true, selected })}>
        <ReviewedIcon reviewed={reviewed} />
        <List.Content>
            <List.Header><NodeFileLink {...props} /></List.Header>
            <NodeFileStats stats={stats} />
        </List.Content>
    </List.Item>;
}

interface FolderNodeProps extends NodeProps {
    folder: FolderTreeNode<FileToReview>;
}

const FolderNode = (props: FolderNodeProps): JSX.Element => {
    const baseProps: NodeProps = {
        reviewId: props.reviewId,
        selectedFile: props.selectedFile,
        reviewedFiles: props.reviewedFiles,
        fileStats: props.fileStats
    };

    const folders = props.folder.folders.map(n => <FolderNode key={nestedName(n)} folder={n} {...baseProps}/>);
    const files = props.folder.files.map(n => <FileNode key={n.name} file={n} {...baseProps} />);

    const folderName = [...props.folder.nestElements, props.folder.name].join('/');

    return <List.Item className="folder">
        <List.Icon name='folder' />
        <List.Content>
            <List.Header>{folderName}</List.Header>
            <List.List>
                {folders}
                {files}
            </List.List>
        </List.Content>
    </List.Item>;
}

interface StateProps {
    nodeBase: NodeProps;
    fileList: FileToReview[];
}

const ReviewFilesTree = (props: StateProps): JSX.Element => {
    let root = buildTree(props.fileList, f => f.reviewFile.newPath);
    root = sortTree(root);
    root = shortTree(root);
    const folders = root.folders.map(n => <FolderNode key={nestedName(n)} folder={n} {...props.nodeBase} />);
    const files = root.files.map(n => <FileNode key={n.name} file={n} {...props.nodeBase} />);

    return <List className="review-files-tree">
        {folders}
        {files}
    </List>
};

const setStatsForUnpublished = (result: { [fileId: string]: FileStats }, item: FileDiscussion) => {
    switch (item.state) {
        case DiscussionState.NeedsResolution:
            result[item.fileId].newNeedsResolution++;
            break;
        case DiscussionState.GoodWork:
            result[item.fileId].newGoodWork++;
            break;
        case DiscussionState.NoActionNeeded:
            result[item.fileId].newNoActionNeeded++;
            break;
    }
};

const setStatsForPublished = (result: { [fileId: string]: FileStats }, item: FileDiscussion) => {
    switch (item.state) {
        case DiscussionState.NeedsResolution:
            result[item.fileId].needsResolution++;
            break;
        case DiscussionState.GoodWork:
            result[item.fileId].goodWork++;
            break;
        case DiscussionState.NoActionNeeded:
            result[item.fileId].noActionNeeded++;
            break;
        case DiscussionState.Resolved:
            result[item.fileId].resolved++;
            break;
    }
};

const buildFileStats = (review: ReviewState) => {
    const result: { [fileId: string]: FileStats } = {};

    for (let item of review.unpublishedFileDiscussions) {
        if(!result[item.fileId]) {
            result[item.fileId] = {...emptyFileStats};
        }

        setStatsForUnpublished(result, item);
    }

    for (let item of review.currentReview.fileDiscussions) {
        if(!result[item.fileId]) {
            result[item.fileId] = {...emptyFileStats};
        }

        setStatsForPublished(result, item);
    }

    return result;
};

export default connect(
    (state: RootState): StateProps => ({
        fileList: state.review.currentReview.filesToReview,
        nodeBase: {
            reviewId: state.review.currentReview.reviewId,
            selectedFile: state.review.selectedFile != null ? state.review.selectedFile.fileId : null,
            reviewedFiles: state.review.reviewedFiles,
            fileStats: buildFileStats(state.review)
        }
    })
)(ReviewFilesTree);