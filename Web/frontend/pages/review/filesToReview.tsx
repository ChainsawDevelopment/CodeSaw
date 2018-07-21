import * as React from "react";
import Table from '@ui/collections/Table';
import { connect } from "react-redux";
import { RootState } from "../../rootState";
import { FileToReview, ReviewFiles, ReviewFile } from "../../api/reviewer";

const FileRow = (props: { file: FileToReview }) => {
    const { file } = props;

    return (
        <Table.Row>
            <Table.Cell>{file.path.newPath}</Table.Cell>
            <Table.Cell>{file.previous}</Table.Cell>
            <Table.Cell>{file.current}</Table.Cell>
            <Table.Cell>{file.hasChanges ? "YES" : "NO"}</Table.Cell>
        </Table.Row>
    );
}

interface StateProps {
    files: ReviewFile[];
}

type Props = StateProps;

const filesToReview = (props: Props): JSX.Element => {
    const files = props.files.map(f => <FileRow key={f.review.path.newPath} file={f.review} />)

    return (
        <div className="files-to-review">
            <Table definition celled compact striped textAlign='center'>
                <Table.Header>
                    <Table.Row>
                        <Table.HeaderCell></Table.HeaderCell>
                        <Table.HeaderCell>Previous</Table.HeaderCell>
                        <Table.HeaderCell>Current</Table.HeaderCell>
                        <Table.HeaderCell>Has changes?</Table.HeaderCell>
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {files}
                </Table.Body>
            </Table>
        </div>
    );
}

export default connect(
    (state: RootState): StateProps => ({
        files: Object.keys(state.review.currentReview.files).map(f => state.review.currentReview.files[f])
    })
)(filesToReview);