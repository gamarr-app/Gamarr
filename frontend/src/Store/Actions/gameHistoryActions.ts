import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { AppDispatch, createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

interface HistoryItem {
  id: number;
  [key: string]: unknown;
}

interface FetchPayload {
  gameId?: number;
  [key: string]: unknown;
}

interface MarkAsFailedPayload {
  historyId: number;
  gameId: number;
}

//
// Variables

export const section = 'gameHistory';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null as unknown,
  items: [] as HistoryItem[],
};

//
// Actions Types

export const FETCH_GAME_HISTORY = 'gameHistory/fetchGameHistory';
export const CLEAR_GAME_HISTORY = 'gameHistory/clearGameHistory';
export const GAME_HISTORY_MARK_AS_FAILED =
  'gameHistory/gameHistoryMarkAsFailed';

//
// Action Creators

export const fetchGameHistory = createThunk(FETCH_GAME_HISTORY);
export const clearGameHistory = createAction(CLEAR_GAME_HISTORY);
export const gameHistoryMarkAsFailed = createThunk(GAME_HISTORY_MARK_AS_FAILED);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_GAME_HISTORY]: function (
    _getState: () => AppState,
    payload: FetchPayload,
    dispatch: AppDispatch
  ) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/history/game',
      data: payload,
    }).request;

    promise.done((data: HistoryItem[]) => {
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

  [GAME_HISTORY_MARK_AS_FAILED]: function (
    _getState: () => AppState,
    payload: MarkAsFailedPayload,
    dispatch: AppDispatch
  ) {
    const { historyId, gameId } = payload;

    const promise = createAjaxRequest({
      url: `/history/failed/${historyId}`,
      method: 'POST',
      dataType: 'json',
    }).request;

    promise.done(() => {
      dispatch(fetchGameHistory({ gameId }));
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [CLEAR_GAME_HISTORY]: (state: typeof defaultState) => {
      return Object.assign({}, state, defaultState);
    },
  },
  defaultState,
  section
);
