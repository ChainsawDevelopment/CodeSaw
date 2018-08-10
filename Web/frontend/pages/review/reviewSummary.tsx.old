import { connect } from 'react-redux';
import * as React from "react";
import { RootState } from '../../rootState';
import Table from '@ui/collections/Table';
import Label from '@ui/elements/Label';
import List from '@ui/elements/List';
import Popup from '@ui/modules/Popup';
import { ReviewInfo, ReviewId, FileToReview, ReviewFiles, ReviewFile, RevisionId } from '../../api/reviewer';

import "./reviewSummary.less";
import { FileLink } from './FileLink';
import * as classNames from 'classnames';

const FileRevision = (props: { reviewers: string[] }) => {
    if (props.reviewers.length == 0) {
        return null;
    }

    const label = (
        <Label>
            {props.reviewers.length}
        </Label>
    );

    const reviewers = props.reviewers.map(u => (
        <List.Item key={u} icon='user secret' content={u} />
    ));

    return (
        <Popup trigger={label}>
            <List>
                {reviewers}
            </List>
        </Popup>
    )
};

const FileRow = (props: { revisions: number[]; hasProvisional: boolean; reviewId: ReviewId; file2: ReviewFile; headCommit: string }) => {
    const isRevisionMatch = (left: RevisionId, right: RevisionId) => {
        return left == right 
            || (left == 'provisional' && right == props.headCommit)
            || (right == 'provisional' && left == props.headCommit);
    };

    const classes = (revision: number | 'provisional') => classNames({
        'revision-status': true,
        'current': isRevisionMatch(revision, props.file2.review.current),
        'previous': isRevisionMatch(revision, props.file2.review.previous),
    });

    const fileStatus = props.file2.summary;

    const revisionStatuses = props.revisions.map(r =>
        <Table.Cell key={r} className={classes(r)}>
            <div><FileRevision reviewers={fileStatus.revisionReviewers[r] || []} /></div>
        </Table.Cell>
    );

    if (props.hasProvisional) {
        revisionStatuses.push(<Table.Cell key={'provisional'} className={classes('provisional')}><div>&nbsp;</div></Table.Cell>);
    }

    return (
        <Table.Row className='file-summary-row'>
            <Table.Cell textAlign='left' className='file-path'>
                <FileLink reviewId={props.reviewId} path={props.file2.review.path} />
            </Table.Cell>
            {revisionStatuses}
        </Table.Row>
    );
}

interface OwnProps {
    reviewId: ReviewId;
}

interface StateProps {
    revisions: number[];
    hasProvisional: boolean;
    files: ReviewFiles;
    headCommit: string;
}

type Props = StateProps & OwnProps;


const reviewSummary = (props: Props) => {
    const headers = props.revisions.map(i => <Table.HeaderCell key={i} className='revision'>{i}</Table.HeaderCell>);

    if (props.hasProvisional) {
        headers.push(<Table.HeaderCell key={'provisional'} className='revision'>&perp;</Table.HeaderCell>);
    }

    const files = Object.keys(props.files).map(f=>props.files[f]);

    const rows = files.map(f => <FileRow
        key={f.review.path.newPath}
        file2={f}
        revisions={props.revisions}
        hasProvisional={props.hasProvisional}
        reviewId={props.reviewId}
        headCommit={props.headCommit}
    />);

    return (
        <div className='review-summary'>
            <Table definition celled compact striped textAlign='center'>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell />
                        {headers}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {rows}
                </Table.Body>
            </Table>
        </div>);
};

const mapStateToProps = (state: RootState): StateProps => ({
    revisions: state.review.currentReview.pastRevisions.map(r => r.number),
    hasProvisional: state.review.currentReview.hasProvisionalRevision,
    files: state.review.currentReview.files,
    headCommit: state.review.currentReview.headCommit
});

export default connect(
    mapStateToProps
)(reviewSummary)