import { connect } from 'react-redux';
import * as React from "react";
import { RootState } from '../../rootState';
import Table from 'semantic-ui-react/dist/commonjs/collections/Table';
import Label from 'semantic-ui-react/dist/commonjs/elements/Label';
import List from 'semantic-ui-react/dist/commonjs/elements/List';
import Popup from 'semantic-ui-react/dist/commonjs/modules/Popup';
import * as PathPairs from '../../pathPair';
import { ReviewInfo } from '../../api/reviewer';

import "./reviewSummary.less";

const FileRevision = (props: {reviewers: string[]}) => {
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

const FileRow = (props: { file: PathPairs.PathPair, revisions: number[]; summary: ReviewInfo }) => {
    const fileStatus = props.summary.reviewSummary.find(x => x.file == props.file.newPath) || {revisions: {}};
    
    const revisionStatuses = props.revisions.map(r => 
        <Table.Cell key={r} className='revision-status'>
            <FileRevision reviewers={fileStatus.revisions[r] || []}/>
        </Table.Cell>
    );

    return (
        <Table.Row className='file-summary-row'>
            <Table.Cell textAlign='left' className='file-path'>{props.file.newPath}</Table.Cell>
            {revisionStatuses}
        </Table.Row>
    );
}

interface StateProps {
    revisions: number[];
    files: PathPairs.List;
    summary: ReviewInfo;
}

type Props = StateProps;


const reviewSummary = (props: Props) => {
    const headers = props.revisions.map(i => <Table.HeaderCell key={i} className='revision'>{i}</Table.HeaderCell>)

    const rows = props.files.map(f => <FileRow key={f.newPath} file={f} revisions={props.revisions} summary={props.summary} />);

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
    files: state.review.rangeInfo ? state.review.rangeInfo.changes.map(f => f.path) : [],
    summary: state.review.currentReview
});

export default connect(
    mapStateToProps
)(reviewSummary)