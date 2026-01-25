import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

export const section = 'paths';

interface Directory {
  path: string;
  name: string;
}

interface FileInfo {
  path: string;
  name: string;
}

export interface PathsState {
  currentPath: string;
  isPopulated: boolean;
  isFetching: boolean;
  error: unknown;
  directories: Directory[];
  files: FileInfo[];
  parent: string | null;
}

interface FetchPathsPayload {
  path: string;
  allowFoldersWithoutTrailingSlashes?: boolean;
  includeFiles?: boolean;
}

interface UpdatePathsPayload {
  path: string;
  directories: Directory[];
  files: FileInfo[];
  parent: string | null;
}

export const defaultState: PathsState = {
  currentPath: '',
  isPopulated: false,
  isFetching: false,
  error: null,
  directories: [],
  files: [],
  parent: null,
};

export const FETCH_PATHS = 'paths/fetchPaths';
export const UPDATE_PATHS = 'paths/updatePaths';
export const CLEAR_PATHS = 'paths/clearPaths';

export const fetchPaths = createThunk(FETCH_PATHS);
export const updatePaths = createAction(UPDATE_PATHS);
export const clearPaths = createAction(CLEAR_PATHS);

export const actionHandlers = handleThunks({
  [FETCH_PATHS]: function (
    _getState: () => AppState,
    payload: FetchPathsPayload,
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isFetching: true }));

    const {
      path,
      allowFoldersWithoutTrailingSlashes = false,
      includeFiles = false,
    } = payload;

    const promise = createAjaxRequest({
      url: '/filesystem',
      data: {
        path,
        allowFoldersWithoutTrailingSlashes,
        includeFiles,
      },
    }).request;

    promise.done((data: Omit<UpdatePathsPayload, 'path'>) => {
      dispatch(updatePaths({ ...data, path }));

      dispatch(
        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null,
        })
      );
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        set({
          section,
          isFetching: false,
          isPopulated: false,
          error: xhr,
        })
      );
    });
  },
});

export const reducers = createHandleActions(
  {
    [UPDATE_PATHS]: (
      state: PathsState,
      { payload }: { payload: UpdatePathsPayload }
    ) => {
      const newState = Object.assign({}, state);

      newState.currentPath = payload.path;
      newState.directories = payload.directories;
      newState.files = payload.files;
      newState.parent = payload.parent;

      return newState;
    },

    [CLEAR_PATHS]: (state: PathsState) => {
      const newState = Object.assign({}, state);

      newState.currentPath = '';
      newState.directories = [];
      newState.files = [];
      newState.parent = '';

      return newState;
    },
  },
  defaultState,
  section
);
