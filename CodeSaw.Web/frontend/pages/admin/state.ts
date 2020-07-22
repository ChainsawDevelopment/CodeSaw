import actionCreatorFactory, { AnyAction } from 'typescript-fsa';
import { ProjectInfo } from '../../api/reviewer';

const createAction = actionCreatorFactory('ADMIN');

export interface AdminState {
    projects: ProjectInfo[];
}

export const loadProjects = createAction('LOAD_PROJECTS');
export const projectsLoaded = createAction<ProjectInfo[]>('PROJECTS_LOADED');

export const setupProjectHooks = createAction<{ projectId: number }>('SETUP_PROJECT_HOOKS');

const initialState: AdminState = {
    projects: [],
};

export const adminReducer = (state: AdminState = initialState, action: AnyAction): AdminState => {
    if (projectsLoaded.match(action)) {
        return {
            ...state,
            projects: action.payload,
        };
    }

    return state;
};
