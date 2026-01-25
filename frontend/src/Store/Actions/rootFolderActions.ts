import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';

export const section = 'rootFolders';

export interface RootFoldersState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isSaving: boolean;
  saveError: unknown;
  items: unknown[];
}

interface AddRootFolderPayload {
  path: string;
}

export const defaultState: RootFoldersState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  items: [],
};

export const FETCH_ROOT_FOLDERS = 'rootFolders/fetchRootFolders';
export const ADD_ROOT_FOLDER = 'rootFolders/addRootFolder';
export const DELETE_ROOT_FOLDER = 'rootFolders/deleteRootFolder';

export const fetchRootFolders = createThunk(FETCH_ROOT_FOLDERS);
export const addRootFolder = createThunk(ADD_ROOT_FOLDER);
export const deleteRootFolder = createThunk(DELETE_ROOT_FOLDER);

export const actionHandlers = handleThunks({
  [FETCH_ROOT_FOLDERS]: createFetchHandler('rootFolders', '/rootFolder'),

  [DELETE_ROOT_FOLDER]: createRemoveItemHandler('rootFolders', '/rootFolder'),

  [ADD_ROOT_FOLDER]: function (
    _getState: () => AppState,
    payload: AddRootFolderPayload,
    dispatch: Dispatch
  ) {
    const path = payload.path;

    dispatch(
      set({
        section,
        isSaving: true,
      })
    );

    const promise = createAjaxRequest({
      url: '/rootFolder',
      method: 'POST',
      data: JSON.stringify({ path }),
      dataType: 'json',
    }).request;

    promise.done((data: Record<string, unknown>) => {
      dispatch(
        batchActions([
          updateItem({
            section,
            ...data,
          }),

          set({
            section,
            isSaving: false,
            saveError: null,
          }),
        ])
      );
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        set({
          section,
          isSaving: false,
          saveError: xhr,
        })
      );
    });
  },
});

export const reducers = createHandleActions({}, defaultState, section);
