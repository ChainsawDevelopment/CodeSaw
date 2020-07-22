import * as React from 'react';
import ReviewMode from '../reviewMode';
import Menu from '@ui/collections/Menu';
import Popup from '@ui/modules/Popup';
import Icon from '@ui/elements/Icon';
import ReviewMark from '../reviewMark';
import { ReviewId, FileToReview, FileDiff } from '@api/reviewer';
import { FileLink } from '../FileLink';
import { PathPair } from '@src/pathPair';
import vscodeLogo from '../../../assets/vscode.png';

export const ToggleReviewed = (props: { isReviewed: boolean; onChange: (newState: boolean) => void }): JSX.Element => {
    return (
        <ReviewMode>
            <ReviewMode.Reviewer>
                <Menu.Item fitted>
                    <Popup
                        trigger={<ReviewMark reviewed={props.isReviewed} onClick={props.onChange} />}
                        content="Toggle review status"
                    />
                </Menu.Item>
            </ReviewMode.Reviewer>
            <ReviewMode.Author>
                <Menu.Item fitted>
                    <Icon circular inverted name="eye" color="grey" />
                </Menu.Item>
            </ReviewMode.Author>
        </ReviewMode>
    );
};

export const RefreshDiff = (props: { onRefresh: () => void }): JSX.Element => {
    return (
        <Menu.Item fitted>
            <Popup
                trigger={<Icon onClick={props.onRefresh} name="redo" circular link color="blue"></Icon>}
                content="Refresh file diff"
            />
        </Menu.Item>
    );
};

export const FileNavigation = (props: {
    reviewId: ReviewId;
    prevFile: FileToReview;
    nextFile: FileToReview;
}): JSX.Element => {
    const { prevFile, nextFile } = props;

    return (
        <Menu.Item fitted>
            {prevFile && (
                <Popup
                    trigger={
                        <FileLink reviewId={props.reviewId} fileId={prevFile.fileId}>
                            <Icon name="step backward" circular link />
                        </FileLink>
                    }
                    content="Previous unreviewed file"
                />
            )}
            {nextFile && (
                <Popup
                    trigger={
                        <FileLink reviewId={props.reviewId} fileId={nextFile.fileId}>
                            <Icon name="step forward" circular link />
                        </FileLink>
                    }
                    content="Next unreviewed file"
                />
            )}
        </Menu.Item>
    );
};

export const FilePath = (props: { path: PathPair }): JSX.Element => (
    <Menu.Item fitted key="file-path">
        <span className="file-path">{props.path.newPath}</span>
    </Menu.Item>
);

export const DownloadDiff = (props: { diff: FileDiff }): JSX.Element => {
    const downloadFile = () => {
        const content = JSON.stringify({
            current: props.diff.contents.review.current,
            previous: props.diff.contents.review.previous,
        });
        const f = new File([content], 'state.json', { type: 'application/octet-stream' });
        const url = window.URL.createObjectURL(f);
        console.log(url);
        const a = document.createElement('a');
        a.href = url;
        a.click();
        window.URL.revokeObjectURL(url);
    };

    return (
        <Menu.Item fitted key="download-diff">
            <Popup
                trigger={<Icon onClick={downloadFile} name="download" circular link color="blue"></Icon>}
                content="Download"
            />
        </Menu.Item>
    );
};

export const OpenVSCode = (props: { workspace: string; path: PathPair }): JSX.Element => {
    const workspacePath = encodeURI(props.workspace.trim().replace('\\', '/').replace(/\/+$/, ''));

    return (
        <Menu.Item fitted key="vscode-diff">
            <Popup
                trigger={
                    <img
                        src={vscodeLogo}
                        className="vscode-icon"
                        onClick={() => window.open(`vscode://file/${workspacePath}/${props.path.newPath}`)}
                    />
                }
                content="Open in VS Code"
            />
        </Menu.Item>
    );
};
