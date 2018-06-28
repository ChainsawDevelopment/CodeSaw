import actionCreatorFactory, { AnyAction } from "typescript-fsa";
import { ProjectInfo } from "../../api/reviewer";

const createAction = actionCreatorFactory('ADMIN');

export interface AdminState {
    projects: ProjectInfo[];
}

export const loadProjects = createAction('LOAD_PROJECTS');
export const projectsLoaded = createAction<ProjectInfo[]>('PROJECTS_LOADED');

const initialState: AdminState = {
    projects: []
}

export const adminReducer = (state: AdminState = initialState, action: AnyAction) => {
    if (projectsLoaded.match(action)) {
        return {
            ...state,
            projects: action.payload
        };
    }    

    return state;
}