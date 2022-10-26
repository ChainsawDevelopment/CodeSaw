import { Commit } from "@api/reviewer";
import { RootState } from "@src/rootState";
import * as React from "react";
import { connect } from 'react-redux';
import UIComment from '@ui/views/Comment';

const CommitView = (props: {commit: Commit}) => {
    return <UIComment>
        <UIComment.Content>
            <UIComment.Author>{props.commit.authorName}</UIComment.Author>
            <UIComment.Metadata>{props.commit.id.slice(0, 7)} at {new Date(props.commit.createdAt).toLocaleString()}</UIComment.Metadata>
            <UIComment.Text>
                <pre>
                {props.commit.message}
                </pre>
            </UIComment.Text>
        </UIComment.Content>
    </UIComment>
}

interface StateProps {
    commits: Commit[];
}

const Commits = (props: StateProps) => {
    return <UIComment.Group>
        {props.commits.map(c => <CommitView key={c.id} commit={c} />)}
    </UIComment.Group>;
}

export default connect(
    (state: RootState): StateProps => ({
        commits: state.review.currentReview.commits,
    })
)(Commits);