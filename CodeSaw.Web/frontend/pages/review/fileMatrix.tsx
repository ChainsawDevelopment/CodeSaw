import { connect } from "react-redux";
import { RootState } from "../../rootState";
import * as React from "react";
import Label from '@ui/elements/Label';
import Popup from '@ui/modules/Popup';
import Icon from '@ui/elements/Icon';
import Checkbox from '@ui/modules/Checkbox';

import Table from '@ui/collections/Table';
import * as PathPairs from "../../pathPair";
import * as classNames from "classnames";

import "./fileMatrix.less";
import { FileToReview, ReviewId, FileDiscussion, FileMatrixRevision, FileMatrixEntry } from "../../api/reviewer";

import { FileLink } from './FileLink';
import { RevisionId } from "@api/revisionId";

type ReviewMark = 'outside' | 'previous' | 'inside' | 'current' | 'single';

const FileDiscussionSummary = (props: {discussions:  FileDiscussion[]}): JSX.Element => {
    const { discussions } = props;

    const unresolved = discussions.filter(f=>f.state == 'NeedsResolution');

    const label = <Label as='span'>{unresolved.length}/{discussions.length}</Label>;

    let content: JSX.Element[] = [];

    if (unresolved.length > 0) {
        content = [
            ...content,
            <span key='unresolved-title'>Discussions that needs resolution:</span>,
            <ul key='unresolved-list'>
                {unresolved.map(d => <li key={d.id}>line {d.lineNumber} from {d.comment.author.name}</li>)}
            </ul>

        ];
    }

    if (content.length == 0) {
        content.push(<span key='all-resolved'>All discussions resolved!</span>);
    }

    return (
        <Popup trigger={label}>
            {content}
        </Popup>
    );
}

const ReviewersSummary = (props: {reviewers: string[]}): JSX.Element => {
    const {reviewers} = props;

    if (reviewers.length == 0) {
        return null;
    }

    const label = <Icon name='user' as='i' />;

    return (
        <Popup trigger={label}>
            {reviewers.length} reviewers seen this version
            <ul>
                {reviewers.map(r => <li key={r}>{r}</li>)}
            </ul>
        </Popup>
    );
}

const MatrixCell = (props: { revision: FileMatrixRevision; reviewMark: ReviewMark; discussions: FileDiscussion[]; reviewers: string[] }): JSX.Element => {
    const { revision, reviewMark, reviewers } = props;

    const classes = classNames({
        'file-revision': true,
        unchanged: revision.isUnchanged,
        changed: !(revision.isNew || revision.isDeleted || revision.isRenamed) && !revision.isUnchanged,
        new: revision.isNew,
        deleted: revision.isDeleted,
        renamed: revision.isRenamed,
    });

    const reviewClasses = classNames({
        'review-mark': true,
        'review-previous': reviewMark == 'previous' || reviewMark == 'single',
        'review-inside': reviewMark != 'outside',
        'review-current': reviewMark == 'current' || reviewMark == 'single',
    });

    let discussions: JSX.Element = null;

    if (props.discussions.length > 0) {

        discussions = (
            <FileDiscussionSummary discussions={props.discussions} />
        );
    }

    return (
        <Table.Cell className={classes}>
            <div className={reviewClasses}>
            {discussions}
            <ReviewersSummary reviewers={reviewers} />
            </div>
        </Table.Cell>
    )
};

const MatrixRow = (props: { file: FileMatrixEntry; review: FileToReview, reviewId: ReviewId, discussions: FileDiscussion[] }): JSX.Element => {
    const { file } = props.file;
    const { review, reviewId } = props;

    const revisions = [
        {
            isDeleted: false,
            isNew: false,
            isRenamed: false,
            isUnchanged: true,
            revision: RevisionId.Base,
            file: PathPairs.make(props.file.file.oldPath),
            reviewers: []
        },
        ...props.file.revisions
    ];

    const revisionCells = [];

    let reviewMark: ReviewMark = 'outside';

    for (let r of revisions) {
        if(reviewMark == 'previous') {
            reviewMark = 'inside';
        } else if (reviewMark == 'current') {
            reviewMark = 'outside';
        } else if (reviewMark == 'single') {
            reviewMark = 'outside';
        }

        if(RevisionId.equal(r.revision, review.previous) && RevisionId.equal(r.revision, review.current)) {
            reviewMark = 'single';
        }
        else if (RevisionId.equal(r.revision, review.previous)) {
            reviewMark = 'previous';
        } else if (RevisionId.equal(r.revision, review.current)) {
            reviewMark = 'current';
        }

        const revisionDiscussions = props.discussions.filter(f => RevisionId.equal(r.revision, f.revision) && f.fileId == props.file.fileId);

        revisionCells.push(<MatrixCell
            key={RevisionId.asString(r.revision)}
            revision={r}
            reviewMark={reviewMark}
            discussions={revisionDiscussions}
            reviewers={r.reviewers}
        />);
    }

    return (
        <Table.Row>
            <Table.Cell key='file' className='file'><FileLink reviewId={reviewId} fileId={props.file.fileId}>{file.newPath}</FileLink> ({props.discussions.length}) </Table.Cell>
            {revisionCells}
        </Table.Row>
    );
};

interface StateProps {
    matrix: FileMatrixEntry[];
    revisions: number[];
    hasProvisional: boolean;
    filesToReview: FileToReview[];
    reviewId: ReviewId;
    fileDiscussions: FileDiscussion[];
}

interface FileMatrixShowOptions {
    hideReviewed: boolean;
    hideWithoutComments: boolean;
}

const DefaultFileMatrixShowOptions: FileMatrixShowOptions = {hideReviewed: false, hideWithoutComments: false};

type Props = StateProps;

const fileMatrixComponent = (props: Props): JSX.Element => {
    const [showOptions, setShowOptions] = React.useState(DefaultFileMatrixShowOptions);

    const headers = props.revisions.map(i => <Table.HeaderCell key={i} className='revision'>{i}</Table.HeaderCell>);

    headers.unshift(<Table.HeaderCell key={'base'} className='revision'>&perp;</Table.HeaderCell>);

    if (props.hasProvisional) {
        headers.push(<Table.HeaderCell key={'provisional'} className='revision' style={{ fontStyle: 'italic' }}>P</Table.HeaderCell>);
    }

    const rows = [];
    for (let entry of props.matrix) {
        const review = props.filesToReview.find(f => PathPairs.equal(f.reviewFile, entry.file));
        if (showOptions.hideReviewed && RevisionId.equal(review.previous, review.current)) {
            continue;
        }

        if (showOptions.hideWithoutComments) {
            const hasDiscussionsForFile = props.fileDiscussions.some(f => f.fileId == entry.fileId);
            if (!hasDiscussionsForFile) {
                continue;
            }
        }

        rows.push(<MatrixRow key={entry.file.newPath} file={entry} review={review} reviewId={props.reviewId} discussions={props.fileDiscussions}/>);
    }

    return (
        <div className='file-matrix'>
            <Table definition celled compact striped collapsing textAlign='center'>
                <Table.Header>
                    <Table.Row>
                        <Table.Cell textAlign='left'>
                            <Checkbox toggle label="Hide reviewed" 
                                onChange={(_, e) => setShowOptions(showOptions => ({...showOptions, hideReviewed: e.checked}))}
                            />
                            <br/>
                            <Checkbox toggle label="Hide without comments" 
                                onChange={(_, e) => setShowOptions(showOptions => ({...showOptions, hideWithoutComments: e.checked}))}
                            />
                        </Table.Cell>
                        {headers}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {rows}
                </Table.Body>
            </Table>
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    matrix: state.review.currentReview.fileMatrix,
    revisions: state.review.currentReview.pastRevisions.map(r => r.number),
    hasProvisional: state.review.currentReview.hasProvisionalRevision,
    filesToReview: state.review.currentReview.filesToReview || [],
    reviewId: state.review.currentReview.reviewId,
    fileDiscussions: state.review.currentReview.fileDiscussions
});

export default connect(mapStateToProps)(fileMatrixComponent);