import { loadProjects, projectsLoaded } from "./state";
import { ReviewerApi, ProjectInfo } from "../../api/reviewer";
import { take, put } from "redux-saga/effects";

function* loadProjectsSaga() {
    const api = new ReviewerApi();

    for (; ;) {
        const action = yield take(loadProjects);

        const projects: ProjectInfo[] = yield api.getProjects();

        yield put(projectsLoaded(projects));
    }
}

export default [
    loadProjectsSaga
];