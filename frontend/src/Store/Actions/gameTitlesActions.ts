import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

interface TitleItem {
  id: number;
  title: string;
  [key: string]: unknown;
}

interface FetchPayload {
  gameId?: number;
  [key: string]: unknown;
}

//
// Variables

export const section = 'gameTitles';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null as unknown,
  items: [] as TitleItem[],
};

//
// Actions Types

export const FETCH_GAME_TITLES = 'gameTitles/fetchGameTitles';

//
// Action Creators

export const fetchGameTitles = createThunk(FETCH_GAME_TITLES);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_GAME_TITLES]: function (
    _getState: () => AppState,
    payload: FetchPayload,
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/alttitle',
      data: payload,
    }).request;

    promise.done((data: TitleItem[]) => {
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

export const reducers = createHandleActions({}, defaultState, section);
