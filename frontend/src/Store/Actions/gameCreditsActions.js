import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'gameCredits';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: []
};

//
// Actions Types

export const FETCH_GAME_CREDITS = 'gameCredits/fetchGameCredits';
export const CLEAR_GAME_CREDITS = 'gameCredits/clearGameCredits';

//
// Action Creators

export const fetchGameCredits = createThunk(FETCH_GAME_CREDITS);
export const clearGameCredits = createAction(CLEAR_GAME_CREDITS);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_GAME_CREDITS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/credit',
      data: payload
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [CLEAR_GAME_CREDITS]: (state) => {
    return Object.assign({}, state, defaultState);
  }

}, defaultState, section);
