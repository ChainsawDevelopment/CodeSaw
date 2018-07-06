import * as React from "react";
import Table from "@ui/collections/Table";
import Button from "@ui/elements/Button";
import { connect } from "react-redux";
import { loadProjects, setupProjectHooks } from "./state";
import { OnMount } from "../../components/OnMount";
import { ProjectInfo } from "../../api/reviewer";
import { RootState } from "../../rootState";

const ProjectRow = (props: { project: ProjectInfo, setupHook: () => void }) => {
    let setupHook;

    if (props.project.canConfigureHooks) {
        setupHook = <Button onClick={props.setupHook}>Setup hooks</Button>
    } else {
        setupHook = <i>No permission to setup hooks for this project</i>;
    }

    return (
        <Table.Row>
            <Table.Cell>{props.project.namespace}/{props.project.name}</Table.Cell>
            <Table.Cell>{setupHook}</Table.Cell>
        </Table.Row>
    );
}

interface DispatchProps {
    loadProjects();
    setupHook(projectId: number);
}

interface StateProps {
    projects: ProjectInfo[];
}

type Props = DispatchProps & StateProps;

const projectsTable = (props: Props) => {
    const projects = props.projects.map(p => {
        const setupHook = () => props.setupHook(p.id);

        return <ProjectRow project={p} setupHook={setupHook} key={p.id} />
    });

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
    (dispatch): DispatchProps => ({
        loadProjects: () => dispatch(loadProjects()),
        setupHook: (projectId) => dispatch(setupProjectHooks({ projectId }))
    })
)(projectsTable);