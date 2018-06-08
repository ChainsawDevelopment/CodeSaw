import * as React from "react";
import { connect } from "react-redux";
import { Dispatch } from "redux";
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import Segment from 'semantic-ui-react/dist/commonjs/elements/Segment';
import { ReviewId, ReviewInfo, RevisionId, RevisionRange, RevisionRangeInfo } from "../../api/reviewer";
import { OnMount } from "../../components/OnMount";
import { RootState } from "../../rootState";
import RangeInfo, { SelectFileForViewHandler, ReviewFileActions } from './rangeInfo';
import "./review.less";
import { FileInfo, loadReviewInfo, createGitLabLink, selectCurrentRevisions, selectFileForView, reviewFile, unreviewFile, publishReview } from "./state";
import VersionSelector from './versionSelector';
import * as PathPairs from "../../pathPair";

interface OwnProps {
    reviewId: ReviewId;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId): void;
    selectRevisionRange(range: RevisionRange): void;
    selectFileForView: SelectFileForViewHandler;
    createGitLabLink(reviewId: ReviewId);
    publishReview(): void;
    reviewFile: ReviewFileActions;
}

interface StateProps {
    currentReview: ReviewInfo;
    currentRange: RevisionRange;
    rangeInfo: RevisionRangeInfo;
    selectedFile: FileInfo;
    reviewedFiles: PathPairs.List;
}

type Props = OwnProps & StateProps & DispatchProps;

const reviewPage = (props: Props): JSX.Element => {
    const provisional: RevisionId[] = props.currentReview.hasProvisionalRevision ? ['provisional'] : [];

    const publishReview = (
        <div>
            <Button onClick={props.publishReview} color='green'>Publish</Button>
        </div>
    );

    const pastRevisions = props.currentReview.pastRevisions.map(i => i.number);
    
    const selectedFile = props.selectedFile ?
        {...props.selectedFile, isReviewed: PathPairs.contains(props.reviewedFiles, props.selectedFile.path)}
        : null;

    return (
        <div id="review-page">
            <OnMount onMount={() => props.loadReviewInfo(props.reviewId)} />

            <h1>Review {props.currentReview.title}</h1>

            <VersionSelector
                available={['base', ...pastRevisions, ...provisional]}
                hasProvisonal={props.currentReview.hasProvisionalRevision}
                range={props.currentRange}
                onSelectRange={props.selectRevisionRange}
            />
            <div>
                <Button onClick={() => props.createGitLabLink(props.reviewId)}>Create link in GitLab</Button>
            </div>
            {publishReview}
            {props.rangeInfo ? (<RangeInfo
                info={props.rangeInfo}
                selectedFile={selectedFile}
                onSelectFileForView={props.selectFileForView}
                reviewFile={props.reviewFile}
                reviewedFiles={props.reviewedFiles}
            />) : null}
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentReview: state.review.currentReview,
    currentRange: state.review.range,
    rangeInfo: state.review.rangeInfo,
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId) => dispatch(loadReviewInfo({ reviewId })),
    selectRevisionRange: range => dispatch(selectCurrentRevisions({ range })),
    selectFileForView: (path) => dispatch(selectFileForView({ path })),
    createGitLabLink: (reviewId) => dispatch(createGitLabLink({ reviewId })),
    publishReview: () => dispatch(publishReview({})),
    reviewFile: {
        review: (path) => dispatch(reviewFile({ path })),
        unreview: (path) => dispatch(unreviewFile({ path })),
    }
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);