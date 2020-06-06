import * as React from "react";
import List from '@ui/elements/List';
import { buildTree, FolderTreeNode, FileTreeNode, sortTree, shortTree } from "@src/treeNode";
import { FileToReview, ReviewId, FileId } from "@api/reviewer";
import { connect } from "react-redux";
import { RootState } from "@src/rootState";
import { FileLink } from "../FileLink";
import { ReviewState } from "../state";
import Label from '@ui/elements/Label';
import Icon from '@ui/elements/Icon';
import * as classNames from "classnames";

const style = require('./ReviewFilesTree.less');

interface FileStats {
    newDiscussions: number;
}

const emptyFileStats: FileStats = {
    newDiscussions: 0
};

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

const NodeFileStats = (props: {stats: FileStats}) => {
    const {stats} = props;
    const description = [];

    if (stats.newDiscussions > 0) {
        description.push(<Label key="new-discussion" size="mini"><Icon name="comment" />{stats.newDiscussions}</Label>);
    }

    if(description.length > 0) {
        return <List.Description>{description}</List.Description>
    } else {
        return <></>
    }
}

const ReviewedIcon = (props: { reviewed: boolean }) => {
    const color = props.reviewed ? 'green' : 'red';

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

    const folders = props.folder.folders.map(n => <FolderNode key={n.name} folder={n} {...baseProps}/>);
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
    const folders = root.folders.map(n => <FolderNode key={n.name} folder={n} {...props.nodeBase} />);
    const files = root.files.map(n => <FileNode key={n.name} file={n} {...props.nodeBase} />);

    return <List className="review-files-tree">
        {folders}
        {files}
    </List>
};

const buildFileStats = (review: ReviewState) => {
    const result: { [fileId: string]: FileStats } = {};

    for (let item of review.unpublishedFileDiscussions) {
        if(!result[item.fileId]) {
            result[item.fileId] = {...emptyFileStats};
        }

        result[item.fileId].newDiscussions++;
    }

    return result;
}

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