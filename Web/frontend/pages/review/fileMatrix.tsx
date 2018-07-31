import { connect } from "react-redux";
import { RootState } from "../../rootState";
import * as React from "react";

import Table from '@ui/collections/Table';
import { PathPair } from "../../pathPair";
import * as classNames from "classnames";

import "./fileMatrix.less";

interface FileMatrixRevision {
    revision: {
        type: string;
        value: number | string;
    };
    isNew: false;
    isRenamed: false;
    isDeleted: false;
    isUnchanged: false;
}

interface FileMatrixEntry {
    file: PathPair;
    revisions: FileMatrixRevision[];
}

type FileMatrix = FileMatrixEntry[];

const MatrixCell = (props: { revision: FileMatrixRevision }): JSX.Element => {
    const { revision } = props;

    const classes = classNames({
        'file-revision': true,
        unchanged: revision.isUnchanged,
        changed: !(revision.isNew || revision.isDeleted || revision.isRenamed) && !revision.isUnchanged,
        new: revision.isNew,
        deleted: revision.isDeleted,
        renamed: revision.isRenamed
    });

    return (
        <Table.Cell className={classes}>

        </Table.Cell>
    )
};

const MatrixRow = (props: { file: FileMatrixEntry }): JSX.Element => {
    const { file, revisions } = props.file;

    return (
        <Table.Row>
            <Table.Cell>{file.newPath}</Table.Cell>
            {revisions.map(r => <MatrixCell key={r.revision.value} revision={r} />)}
        </Table.Row>
    );
};

interface StateProps {
    matrix: FileMatrix;
    revisions: number[];
    hasProvisional: boolean;
}

type Props = StateProps;

const fileMatrixComponent = (props: Props): JSX.Element => {
    const headers = props.revisions.map(i => <Table.HeaderCell key={i} className='revision'>{i}</Table.HeaderCell>);

    if (props.hasProvisional) {
        headers.push(<Table.HeaderCell key={'provisional'} className='revision'>&perp;</Table.HeaderCell>);
    }

    const rows = props.matrix.map(i => <MatrixRow key={i.file.newPath} file={i} />)

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
    matrix: state.review.currentReview.fileMatrix.matrix || [],
    revisions: state.review.currentReview.pastRevisions.map(r => r.number),
    hasProvisional: state.review.currentReview.hasProvisionalRevision,
});

export default connect(mapStateToProps)(fileMatrixComponent);