import * as React from "react";

import { Dispatch } from "redux";
import {
    selectCurrentRevisions,
    selectFileForView,
    loadReviewInfo,
    FileInfo,
    publishReview,
    reviewFile,
    unreviewFile,
    loadComments,
    addComment,
    resolveComment,
    mergePullRequest
} from "./state";
import {
    RevisionRangeInfo,
    ReviewInfo,
    RevisionRange,
    ReviewId,
    RevisionId,
    Comment
} from '../../api/reviewer';
import { OnMount } from "../../components/OnMount";
import { OnPropChanged } from "../../components/OnPropChanged";
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import RangeInfo, { SelectFileForViewHandler, ReviewFileActions } from './rangeInfo';
import MergeApprover from './mergeApprover';
import "./review.less";
import VersionSelector from './versionSelector';
import * as PathPairs from "../../pathPair";
import ReviewSummary from './reviewSummary';
import CommentsView, { CommentsActions } from './commentsView';
import { PathPair, emptyPathPair } from "../../pathPair";

import Divider from 'semantic-ui-react/dist/commonjs/elements/Divider';
import Grid from 'semantic-ui-react/dist/commonjs/collections/Grid';

interface OwnProps {
    reviewId: ReviewId;
    fileName?: string;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId, fileToPreload?: PathPair): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string);
    reviewFile: ReviewFileActions;
    commentActions: CommentsActions;
    publishReview(): void;
}

interface StateProps {
    currentReview: ReviewInfo;
    currentRange: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: FileInfo;
    reviewedFiles: PathPairs.List;
    comments: Comment[];
}

type Props = OwnProps & StateProps & DispatchProps;

class reviewPage extends React.Component<Props> {
    private showFileHandler: () => void;

    onShowFile() {
        if(this.showFileHandler) {
            this.showFileHandler()
        }
    }

    saveShowFileHandler = (showFileHandler: () => void) => {
        this.showFileHandler = showFileHandler;
    }

    scrollToFileWhenHandlerIsAvailable = (showFileHandler: () => void) => {
        this.saveShowFileHandler(showFileHandler);
        showFileHandler();
    }

    onShowFileHandlerAvailable = this.saveShowFileHandler;

    render() {
        const props = this.props;
        const provisional: RevisionId[] = props.currentReview.hasProvisionalRevision ? ['provisional'] : [];

        const pastRevisions = props.currentReview.pastRevisions.map(i => i.number);

        const selectedFile = props.selectedFile ?
            {...props.selectedFile, isReviewed: PathPairs.contains(props.reviewedFiles, props.selectedFile.path)}
            : null;

        const load = () => {
            if (!selectedFile && props.fileName) {
                this.onShowFileHandlerAvailable = this.scrollToFileWhenHandlerIsAvailable;
                props.loadReviewInfo(props.reviewId,{newPath: props.fileName, oldPath: props.fileName});
            } else {        
                this.onShowFileHandlerAvailable = this.saveShowFileHandler;
                props.loadReviewInfo(props.reviewId,);
            }
        };
        return (
            <div id="review-page">
                <Grid centered columns={2}>
                    <Grid.Row>
                        <Grid.Column>
            
                        <OnMount onMount={load} />
                        <OnPropChanged fileName={props.fileName} onPropChanged={() => {
                            props.selectFileForView({newPath: props.fileName, oldPath: props.fileName});
                            this.onShowFile();
                        }} />

                        <h1>Review {props.currentReview.title}</h1>

                        <MergeApprover
                            reviewId={props.reviewId}
                            reviewState={props.currentReview.state}
                            mergePullRequest={props.mergePullRequest}
                        />
                        <Divider />
                        
                        <VersionSelector
                            available={['base', ...pastRevisions, ...provisional]}
                            hasProvisonal={props.currentReview.hasProvisionalRevision}
                            range={props.currentRange}
                            onSelectRange={props.selectRevisionRange}
                        />

                    <ReviewSummary
                        reviewId={props.reviewId}
                    />

                    <CommentsView comments={props.comments} actions={props.commentActions} />

                        </Grid.Column>
                    </Grid.Row>
                </Grid>

                <Divider />

                {props.rangeInfo ? (<RangeInfo
                    info={props.rangeInfo}
                    selectedFile={selectedFile}
                    onSelectFileForView={props.selectFileForView}
                    reviewFile={props.reviewFile}
                    reviewedFiles={props.reviewedFiles}
                    publishReview={props.publishReview}
                    onShowFileHandlerAvailable={this.onShowFileHandlerAvailable}
                    reviewId={props.reviewId}
                />) : null}
            </div>
        );
    }
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentReview: state.review.currentReview,
    currentRange: state.review.range,
    rangeInfo: state.review.rangeInfo,
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles,
    comments: state.review.comments
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: PathPair) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: (path) => dispatch(selectFileForView({ path })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    },
    commentActions: {
        load: () => dispatch(loadComments({})),
        add: (content, filePath, changeKey, needsResolution, parentId) => dispatch(addComment({ content, filePath, changeKey, needsResolution, parentId })),
        resolve: (commentId) => dispatch(resolveComment({ commentId }))
    },
    publishReview: () => dispatch(publishReview({})),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);
