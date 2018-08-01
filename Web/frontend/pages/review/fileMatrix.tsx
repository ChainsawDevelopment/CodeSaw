import { connect } from "react-redux";
import { RootState } from "../../rootState";
import * as React from "react";

import Table from '@ui/collections/Table';
import { PathPair } from "../../pathPair";
import * as PathPairs from "../../pathPair";
import * as classNames from "classnames";

import "./fileMatrix.less";
import { FileToReview } from "../../api/reviewer";

interface FileMatrixRevision {
    revision: {
        type: string;
        value: number | string;
    };
    isNew: boolean;
    isRenamed: boolean;
    isDeleted: boolean;
    isUnchanged: boolean;
}

interface FileMatrixEntry {
    file: PathPair;
    revisions: FileMatrixRevision[];
}

type FileMatrix = FileMatrixEntry[];

type ReviewMark = 'outside' | 'previous' | 'inside' | 'current' | 'single';

const MatrixCell = (props: { revision: FileMatrixRevision; reviewMark: ReviewMark }): JSX.Element => {
    const { revision, reviewMark } = props;

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

    return (
        <Table.Cell className={classes}>
            <div className={reviewClasses}/>
        </Table.Cell>
    )
};

const MatrixRow = (props: { file: FileMatrixEntry; review: FileToReview }): JSX.Element => {
    const { file } = props.file;
    const { review } = props;

    const revisions = props.file.revisions.concat([]);

    const revisionCells = [];

    revisions.unshift({
        isDeleted: false,
        isNew: false,
        isRenamed: false,
        isUnchanged: true,
        revision: {
            type: 'base',
            value: 'base'
        }
    });

    let reviewMark: ReviewMark = 'outside';

    for (let r of revisions) {
        if(reviewMark == 'previous') {
            reviewMark = 'inside';
        } else if (reviewMark == 'current') {
            reviewMark = 'outside';
        } else if (reviewMark == 'single') {
            reviewMark = 'outside';
        }
        
        if (r.revision.value == review.previous && r.revision.value == review.current) {
            reviewMark = 'single';
        }
        else if (r.revision.value == review.previous) {
            reviewMark = 'previous';
        } else if (r.revision.value == review.current) {
            reviewMark = 'current';
        }  

        revisionCells.push(<MatrixCell
            key={r.revision.value}
            revision={r}
            reviewMark={reviewMark}
        />);
    }

    return (
        <Table.Row>
            <Table.Cell key='file'>{file.newPath}</Table.Cell>
            {revisionCells}
        </Table.Row>
    );
};

interface StateProps {
    matrix: FileMatrix;
    revisions: number[];
    hasProvisional: boolean;
    filesToReview: FileToReview[];
}

type Props = StateProps;

const fileMatrixComponent = (props: Props): JSX.Element => {
    const headers = props.revisions.map(i => <Table.HeaderCell key={i} className='revision'>{i}</Table.HeaderCell>);

    headers.unshift(<Table.HeaderCell key={'base'} className='revision'>&perp;</Table.HeaderCell>);

    if (props.hasProvisional) {
        headers.push(<Table.HeaderCell key={'provisional'} className='revision' style={{ fontStyle: 'italic' }}>P</Table.HeaderCell>);
    }

    const rows = [];
    for (let entry of props.matrix) {
        const review = props.filesToReview.find(f => PathPairs.equal(f.reviewFile, entry.file));

        rows.push(<MatrixRow key={entry.file.newPath} file={entry} review={review} />);
    }

    return (
        <div className='file-matrix'>
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
        </div>
    );
};

const mapStateToProps = (state: RootState): StateProps => ({
    matrix: state.review.currentReview.fileMatrix || [],
    revisions: state.review.currentReview.pastRevisions.map(r => r.number),
    hasProvisional: state.review.currentReview.hasProvisionalRevision,
    filesToReview: state.review.currentReview.filesToReview || [],
});

export default connect(mapStateToProps)(fileMatrixComponent);