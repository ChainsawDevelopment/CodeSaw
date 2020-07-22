import { PathPair, make } from '../../pathPair';
import { RootState } from '../../rootState';
import { connect } from 'react-redux';
import * as React from 'react';

interface OwnProps {
    fileId: string;
}

interface StateProps {
    fileName: PathPair;
}

const FileName = (props: OwnProps & StateProps): JSX.Element => {
    return <span>{props.fileName.newPath}</span>;
};

const mapStateToProps = (state: RootState, ownProps: OwnProps) => ({
    fileName: state.review.currentReview.fileMatrix.find((f) => f.fileId == ownProps.fileId).file,
});

export default connect(mapStateToProps)(FileName);
