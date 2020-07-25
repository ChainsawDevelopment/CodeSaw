import { loadProjects, projectsLoaded, setupProjectHooks } from './state';
import { ReviewerApi, ProjectInfo } from '../../api/reviewer';
import { take, put } from 'redux-saga/effects';
import { Action } from 'typescript-fsa';
import { startOperation, stopOperation } from '../../loading/saga';

function* loadProjectsSaga(): Generator<any, any, any> {
    const api = new ReviewerApi();

    for (;;) {
        const action = yield take(loadProjects);

        yield startOperation();

        const projects: ProjectInfo[] = yield api.getProjects();

        yield put(projectsLoaded(projects));

        yield stopOperation();
    }
}

function* setupProjectHooksSaga(): Generator<any, any, any> {
    const api = new ReviewerApi();

    for (;;) {
        const action: Action<{ projectId: number }> = yield take(setupProjectHooks);

        yield startOperation();

        yield api.setupProjectHooks(action.payload.projectId);

        yield put(loadProjects());

        yield stopOperation();
    }
}

export default [loadProjectsSaga, setupProjectHooksSaga];
