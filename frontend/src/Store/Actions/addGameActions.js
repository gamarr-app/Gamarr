import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getNewGame from 'Utilities/Game/getNewGame';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import { set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'addGame';
let abortCurrentRequest = null;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isAdding: false,
  isAdded: false,
  addError: null,
  items: [],

  defaults: {
    rootFolderPath: '',
    monitor: 'gameOnly',
    qualityProfileId: 0,
    minimumAvailability: 'released',
    searchForGame: true,
    tags: []
  }
};

export const persistState = [
  'addGame.defaults'
];

//
// Actions Types

export const LOOKUP_GAME = 'addGame/lookupGame';
export const ADD_GAME = 'addGame/addGame';
export const SET_ADD_GAME_VALUE = 'addGame/setAddGameValue';
export const CLEAR_ADD_GAME = 'addGame/clearAddGame';
export const SET_ADD_GAME_DEFAULT = 'addGame/setAddGameDefault';

//
// Action Creators

export const lookupGame = createThunk(LOOKUP_GAME);
export const addGame = createThunk(ADD_GAME);
export const clearAddGame = createAction(CLEAR_ADD_GAME);
export const setAddGameDefault = createAction(SET_ADD_GAME_DEFAULT);

export const setAddGameValue = createAction(SET_ADD_GAME_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Action Handlers

export const actionHandlers = handleThunks({

  [LOOKUP_GAME]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    const { request, abortRequest } = createAjaxRequest({
      url: '/game/lookup',
      data: {
        term: payload.term
      }
    });

    abortCurrentRequest = abortRequest;

    request.done((data) => {
      data = data.map((game) => ({ ...game, internalId: game.id, id: game.igdbId }));

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

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });
  },

  [ADD_GAME]: function(getState, payload, dispatch) {
    dispatch(set({ section, isAdding: true }));

    const igdbId = payload.igdbId;
    const items = getState().addGame.items;
    const newGame = getNewGame(_.cloneDeep(_.find(items, { igdbId })), payload);
    newGame.id = 0;

    const promise = createAjaxRequest({
      url: '/game',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(newGame)
    }).request;

    promise.done((data) => {
      const updatedItem = _.cloneDeep(data);
      updatedItem.internalId = updatedItem.id;
      updatedItem.id = updatedItem.igdbId;
      delete updatedItem.images;

      const actions = [
        updateItem({ section: 'games', ...data }),
        updateItem({ section: 'addGame', ...updatedItem }),

        set({
          section,
          isAdding: false,
          isAdded: true,
          addError: null
        })
      ];

      if (!newGame.collection) {
        dispatch(batchActions(actions));
        return;
      }

      const collectionToUpdate = getState().gameCollections.items.find((collection) => collection.igdbId === newGame.collection.igdbId);

      if (collectionToUpdate) {
        const collectionData = { ...collectionToUpdate, missingGames: Math.max(0, collectionToUpdate.missingGames - 1 ) };
        actions.push(updateItem({ section: 'gameCollections', ...collectionData }));
      }

      dispatch(batchActions(actions));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        isAdded: false,
        addError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ADD_GAME_VALUE]: createSetSettingValueReducer(section),

  [SET_ADD_GAME_DEFAULT]: function(state, { payload }) {
    const newState = getSectionState(state, section);

    newState.defaults = {
      ...newState.defaults,
      ...payload
    };

    return updateSectionState(state, section, newState);
  },

  [CLEAR_ADD_GAME]: function(state) {
    const {
      defaults,
      view,
      ...otherDefaultState
    } = defaultState;

    return Object.assign({}, state, otherDefaultState);
  }

}, defaultState, section);
