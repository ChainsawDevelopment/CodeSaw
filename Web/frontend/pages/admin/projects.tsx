import * as React from "react";
import Table from "@ui/collections/Table";
import { connect } from "react-redux";
import { loadProjects } from "./state";
import { OnMount } from "../../components/OnMount";
import { ProjectInfo } from "../../api/reviewer";
import { RootState } from "../../rootState";

const ProjectRow = (props: { project: ProjectInfo }) => {
    return (
        <Table.Row>
            <Table.Cell>{props.project.namespace}/{props.project.name}</Table.Cell>
        </Table.Row>
    );
}

interface DispatchProps {
    loadProjects();
}

interface StateProps {
    projects: ProjectInfo[];
}

type Props = DispatchProps & StateProps;

const projectsTable = (props: Props) => {
    const projects = props.projects.map(p => < ProjectRow project={p} key={p.id} />)

    return (
        <>
        <OnMount onMount={props.loadProjects} />
        <Table>
            <Table.Header>
                <Table.Row>
                    <Table.HeaderCell>Project</Table.HeaderCell>
                    <Table.HeaderCell>CodeSaw hooks</Table.HeaderCell>
                </Table.Row>
            </Table.Header>
            <Table.Body>
                {projects}
            </Table.Body>
        </Table>
        </>
    );
}

export default connect(
    (state: RootState): StateProps => ({
        projects: state.admin.projects
    }),
    (dispatch):DispatchProps => ({
        loadProjects: () => dispatch(loadProjects())
    })
)(projectsTable);