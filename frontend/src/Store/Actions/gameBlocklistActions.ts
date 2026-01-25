import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

interface BlocklistItem {
  id: number;
  [key: string]: unknown;
}

interface FetchPayload {
  gameId?: number;
  [key: string]: unknown;
}

//
// Variables

export const section = 'gameBlocklist';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null as unknown,
  items: [] as BlocklistItem[],
};

//
// Actions Types

export const FETCH_GAME_BLOCKLIST = 'gameBlocklist/fetchGameBlocklist';
export const CLEAR_GAME_BLOCKLIST = 'gameBlocklist/clearGameBlocklist';

//
// Action Creators

export const fetchGameBlocklist = createThunk(FETCH_GAME_BLOCKLIST);
export const clearGameBlocklist = createAction(CLEAR_GAME_BLOCKLIST);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_GAME_BLOCKLIST]: function (
    _getState: () => AppState,
    payload: FetchPayload,
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/blocklist/game',
      data: payload,
    }).request;

    promise.done((data: BlocklistItem[]) => {
      dispatch(
        batchActions([
          update({ section, data }),

          set({
            section,
            isFetching: false,
            isPopulated: true,
            error: null,
          }),
        ])
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

//
// Reducers

export const reducers = createHandleActions(
  {
    [CLEAR_GAME_BLOCKLIST]: (state: typeof defaultState) => {
      return Object.assign({}, state, defaultState);
    },
  },
  defaultState,
  section
);
