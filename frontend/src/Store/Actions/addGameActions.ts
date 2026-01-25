import _ from 'lodash';
import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
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
let abortCurrentRequest: (() => void) | null = null;

//
// State

interface AddGameDefaults {
  rootFolderPath: string;
  monitor: string;
  qualityProfileId: number;
  minimumAvailability: string;
  searchForGame: boolean;
  tags: number[];
}

export interface AddGameState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isAdding: boolean;
  isAdded: boolean;
  addError: unknown;
  items: unknown[];
  defaults: AddGameDefaults;
}

export const defaultState: AddGameState = {
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
    tags: [],
  },
};

export const persistState = ['addGame.defaults'];

//
// Actions Types

export const LOOKUP_GAME = 'addGame/lookupGame';
export const ADD_GAME = 'addGame/addGame';
export const SET_ADD_GAME_VALUE = 'addGame/setAddGameValue';
export const CLEAR_ADD_GAME = 'addGame/clearAddGame';
export const SET_ADD_GAME_DEFAULT = 'addGame/setAddGameDefault';

//
// Action Creators

interface LookupGamePayload {
  term: string;
}

interface AddGamePayload {
  igdbId: number;
  steamAppId?: number;
  [key: string]: unknown;
}

interface SetAddGameValuePayload {
  [key: string]: unknown;
}

export const lookupGame = createThunk(LOOKUP_GAME);
export const addGame = createThunk(ADD_GAME);
export const clearAddGame = createAction(CLEAR_ADD_GAME);
export const setAddGameDefault =
  createAction<Partial<AddGameDefaults>>(SET_ADD_GAME_DEFAULT);

export const setAddGameValue = createAction(
  SET_ADD_GAME_VALUE,
  (payload: SetAddGameValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [LOOKUP_GAME]: function (
    _getState: () => AppState,
    payload: LookupGamePayload,
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isFetching: true, items: [], isPopulated: false }));

    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    const { request, abortRequest } = createAjaxRequest({
      url: '/game/lookup',
      data: {
        term: payload.term,
      },
    });

    abortCurrentRequest = abortRequest;

    request.done(
      (data: Array<{ id: number; igdbId: number; [key: string]: unknown }>) => {
        const mappedData = data.map((game) => ({
          ...game,
          internalId: game.id,
          id: game.igdbId,
        }));

        dispatch(
          batchActions([
            update({ section, data: mappedData }),

            set({
              section,
              isFetching: false,
              isPopulated: true,
              error: null,
            }),
          ])
        );
      }
    );

    request.fail((xhr: { aborted?: boolean }) => {
      dispatch(
        set({
          section,
          isFetching: false,
          isPopulated: false,
          error: xhr.aborted ? null : xhr,
        })
      );
    });
  },

  [ADD_GAME]: function (
    getState: () => AppState,
    payload: AddGamePayload,
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isAdding: true }));

    const igdbId = payload.igdbId;
    const steamAppId = payload.steamAppId;
    const items = (getState() as unknown as { addGame: AddGameState }).addGame
      .items as Array<{
      igdbId: number;
      steamAppId?: number;
      [key: string]: unknown;
    }>;

    // Find by igdbId if available, otherwise by steamAppId
    let foundGame:
      | { igdbId: number; steamAppId?: number; [key: string]: unknown }
      | undefined = undefined;
    if (igdbId > 0) {
      foundGame = _.find(items, { igdbId });
    } else if (steamAppId && steamAppId > 0) {
      foundGame = _.find(items, { steamAppId });
    }

    const newGame = getNewGame(_.cloneDeep(foundGame), payload) as {
      id: number;
      collection?: { igdbId: number };
      igdbId: number;
      images?: unknown;
    };
    newGame.id = 0;

    const promise = createAjaxRequest({
      url: '/game',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(newGame),
    }).request;

    promise.done(
      (data: {
        id: number;
        igdbId: number;
        images?: unknown;
        [key: string]: unknown;
      }) => {
        const updatedItem: {
          internalId: number;
          id: number;
          images?: unknown;
          [key: string]: unknown;
        } = _.cloneDeep(data);
        updatedItem.internalId = updatedItem.id;
        updatedItem.id = data.igdbId;
        delete updatedItem.images;

        const actions = [
          updateItem({ section: 'games', ...data }),
          updateItem({ section: 'addGame', ...updatedItem }),

          set({
            section,
            isAdding: false,
            isAdded: true,
            addError: null,
          }),
        ];

        if (!newGame.collection) {
          dispatch(batchActions(actions));
          return;
        }

        const state = getState() as unknown as {
          gameCollections: {
            items: Array<{ igdbId: number; missingGames: number }>;
          };
        };
        const collectionToUpdate = state.gameCollections.items.find(
          (collection) => collection.igdbId === newGame.collection!.igdbId
        );

        if (collectionToUpdate) {
          const collectionData = {
            ...collectionToUpdate,
            missingGames: Math.max(0, collectionToUpdate.missingGames - 1),
          };
          actions.push(
            updateItem({ section: 'gameCollections', ...collectionData })
          );
        }

        dispatch(batchActions(actions));
      }
    );

    promise.fail((xhr: unknown) => {
      dispatch(
        set({
          section,
          isAdding: false,
          isAdded: false,
          addError: xhr,
        })
      );
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [SET_ADD_GAME_VALUE]: createSetSettingValueReducer(section),

    [SET_ADD_GAME_DEFAULT]: function (
      state: AddGameState,
      { payload }: { payload: Partial<AddGameDefaults> }
    ) {
      const newState = getSectionState(state, section);

      newState.defaults = {
        ...newState.defaults,
        ...payload,
      };

      return updateSectionState(state, section, newState);
    },

    [CLEAR_ADD_GAME]: function (state: AddGameState) {
      const { defaults, ...otherDefaultState } = defaultState;

      return Object.assign({}, state, otherDefaultState);
    },
  },
  defaultState,
  section
);
