import { createAction } from 'redux-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'extraFiles';

//
// State

export interface ExtraFilesState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: unknown[];
}

export const defaultState: ExtraFilesState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],
};

//
// Actions Types

export const FETCH_EXTRA_FILES = 'extraFiles/fetchExtraFiles';
export const CLEAR_EXTRA_FILES = 'extraFiles/clearExtraFiles';

//
// Action Creators

export const fetchExtraFiles = createThunk(FETCH_EXTRA_FILES);
export const clearExtraFiles = createAction(CLEAR_EXTRA_FILES);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_EXTRA_FILES]: createFetchHandler(section, '/extraFile'),
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [CLEAR_EXTRA_FILES]: (state: ExtraFilesState) => {
      return Object.assign({}, state, defaultState);
    },
  },
  defaultState,
  section
);
