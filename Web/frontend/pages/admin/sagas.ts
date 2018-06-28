import { loadProjects, projectsLoaded, setupProjectHooks } from "./state";
import { ReviewerApi, ProjectInfo } from "../../api/reviewer";
import { take, put } from "redux-saga/effects";
import { Action } from "typescript-fsa";

function* loadProjectsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action = yield take(loadProjects);

        const projects: ProjectInfo[] = yield api.getProjects();

        yield put(projectsLoaded(projects));
    }
}

function* setupProjectHooksSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action: Action<{ projectId: number }> = yield take(setupProjectHooks);

        yield api.setupProjectHooks(action.payload.projectId);

        yield put(loadProjects());
    }
}

export default [
    loadProjectsSaga,
    setupProjectHooksSaga
];