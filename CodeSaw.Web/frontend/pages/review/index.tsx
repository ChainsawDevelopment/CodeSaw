import * as React from "react";
import { Dispatch } from "redux";
import { connect } from "react-redux";
import { History } from "history";

import Divider from '@ui/elements/Divider';
import Grid from '@ui/collections/Grid';

import {
    selectFileForView,
    loadReviewInfo,
    FileInfo,
    mergePullRequest,
} from "./state";
import {
    ReviewInfo,
    ReviewId,
    FileId,
} from '../../api/reviewer';

import { OnMount } from "../../components/OnMount";
import { OnPropChanged } from "../../components/OnPropChanged";
import { UserState, RootState } from "../../rootState";
import { SelectFileForViewHandler } from './rangeInfo';
import "./review.less";
import FileMatrix from './fileMatrix';
import ReviewInfoView from './reviewInfoView';

import { createLinkToFile } from "./FileLink";
import CurrentReviewMode from './currentReviewMode';
import PageTitle from '../../components/PageTitle';

import Header from './sections/header';
import Actions from './sections/actions';
import ReviewDiscussions from './sections/reviewDiscussions';
import File from './sections/file';

interface OwnProps {
    reviewId: ReviewId;
    fileId?: FileId;
    history: History;
}

interface DispatchProps {
    loadReviewInfo(reviewId: ReviewId, fileToPreload?: FileId): void;
    selectFileForView: SelectFileForViewHandler;
    mergePullRequest(reviewId: ReviewId, shouldRemoveBranch: boolean, commitMessage: string): void;
}

interface StateProps {
    currentUser: UserState;
    currentReview: ReviewInfo;
    selectedFile: FileInfo;
    reviewedFiles: FileId[];
    author: UserState;
    reviewMode: 'reviewer' | 'author';
}

type Props = OwnProps & StateProps & DispatchProps;

interface State {
    hideReviewed: boolean;
}


class reviewPage extends React.Component<Props, State> {
    private showFileHandler: () => void;

    constructor(props: Props) {
        super(props);
        this.state = {
            hideReviewed: false,
        };
    }

    onShowFile() {
        if (this.showFileHandler) {
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

        const selectedFile = props.selectedFile ?
            { ...props.selectedFile, isReviewed: props.reviewedFiles.indexOf(props.selectedFile.fileId) >= 0 }
            : null;

        const load = () => {
            if (!selectedFile && props.fileId) {
                this.onShowFileHandlerAvailable = this.scrollToFileWhenHandlerIsAvailable;
                props.loadReviewInfo(props.reviewId, props.fileId);
            } else {
                this.onShowFileHandlerAvailable = this.saveShowFileHandler;
                props.loadReviewInfo(props.reviewId);
            }
        };

        const selectNewFileForView = (fileId: FileId) => {
            if (fileId != null) {
                props.selectFileForView(fileId);

                const fileLink = createLinkToFile(props.reviewId, fileId);
                if (fileLink != window.location.pathname) {
                    props.history.push(fileLink);
                }

                this.onShowFile();
            }
        };

        const selectFileForView = () => {
            const file = props.currentReview.filesToReview.find(f => f.fileId == props.fileId);
            if (file != null) {
                selectNewFileForView(file.fileId);
            }
        };

        const title = (() => {
            if (!props.currentReview.reviewId) {
                return 'Loading review...';
            }

            const { currentReview } = props;
            return `[${currentReview.projectPath}] #${currentReview.reviewId.reviewId} - ${currentReview.title}`;
        })();

        const changeHideReviewed = (hide: boolean) => {
            this.setState({
                hideReviewed: hide
            });
        };

        return (
            <div id="review-page">
                <PageTitle>{title}</PageTitle>
                <CurrentReviewMode.Provider value={props.reviewMode}>
                    <OnMount onMount={load} />
                    <OnPropChanged fileName={props.fileId} onPropChanged={selectFileForView} />

                    <Grid>
                        <Header />

                        <Grid.Row>
                            <Grid.Column>
                                <ReviewInfoView />
                                <Divider />
                            </Grid.Column>
                        </Grid.Row>
                        <Grid.Row columns={1}>
                            <Grid.Column>
                                <Actions onHideReviewedChange={changeHideReviewed} />
                            </Grid.Column>
                        </Grid.Row>
                        <Grid.Row columns={1}>
                            <Grid.Column>
                                <FileMatrix hideReviewed={this.state.hideReviewed} />
                            </Grid.Column>
                        </Grid.Row>
                        <ReviewDiscussions />
                    </Grid>

                    <Divider />

                    <File
                        showFileHandler={this.showFileHandler}
                        reviewId={props.reviewId}
                        fileId={props.fileId}
                        history={props.history}
                        onShowFileHandlerAvailable={this.onShowFileHandlerAvailable}
                    />
                </CurrentReviewMode.Provider>
            </div>
        );
    }
};

const mapStateToProps = (state: RootState): StateProps => ({
    currentUser: state.currentUser,
    currentReview: state.review.currentReview,
    selectedFile: state.review.selectedFile,
    reviewedFiles: state.review.reviewedFiles,
    author: state.review.currentReview.author,
    reviewMode: state.review.currentReview.isAuthor ? 'author' : 'reviewer',
});

const mapDispatchToProps = (dispatch: Dispatch): DispatchProps => ({
    loadReviewInfo: (reviewId: ReviewId, fileToPreload?: string) => dispatch(loadReviewInfo({ reviewId, fileToPreload })),
    selectFileForView: (fileId) => dispatch(selectFileForView({ fileId })),
    mergePullRequest: (reviewId, shouldRemoveBranch, commitMessage) => dispatch(mergePullRequest({ reviewId, shouldRemoveBranch, commitMessage })),
});

export default connect(
    mapStateToProps,
    mapDispatchToProps
)(reviewPage);
