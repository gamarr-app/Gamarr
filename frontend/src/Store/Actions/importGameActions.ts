import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import ImportGameAppState, {
  ImportGameItem,
} from 'App/State/ImportGameAppState';
import Game from 'Game/Game';
import { AppDispatch, createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getNewGame, { NewGamePayload } from 'Utilities/Game/getNewGame';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import { removeItem, set, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import { fetchRootFolders } from './rootFolderActions';

type SelectedGame = Partial<Game> & {
  igdbId: number;
};

interface QueuePayload {
  name: string;
  path: string;
  relativePath: string;
  term: string;
  topOfQueue?: boolean;
}

interface StartPayload {
  start?: boolean;
}

interface ImportPayload {
  ids: string[];
}

interface SetValuePayload {
  id: string;
  [key: string]: unknown;
}

//
// Variables

export const section = 'importGame';
let concurrentLookups = 0;
let abortCurrentLookup: (() => void) | null = null;
const queue: string[] = [];

//
// State

export const defaultState: ImportGameAppState = {
  isLookingUpGame: false,
  isImporting: false,
  isImported: false,
  importError: undefined,
  items: [],
};

//
// Actions Types

export const QUEUE_LOOKUP_GAME = 'importGame/queueLookupGame';
export const START_LOOKUP_GAME = 'importGame/startLookupGame';
export const CANCEL_LOOKUP_GAME = 'importGame/cancelLookupGame';
export const LOOKUP_UNSEARCHED_GAMES = 'importGame/lookupUnsearchedGames';
export const CLEAR_IMPORT_GAME = 'importGame/clearImportGame';
export const SET_IMPORT_GAME_VALUE = 'importGame/setImportGameValue';
export const IMPORT_GAME = 'importGame/importGame';

//
// Action Creators

export const queueLookupGame = createThunk(QUEUE_LOOKUP_GAME);
export const startLookupGame = createThunk(START_LOOKUP_GAME);
export const importGame = createThunk(IMPORT_GAME);
export const lookupUnsearchedGames = createThunk(LOOKUP_UNSEARCHED_GAMES);
export const clearImportGame = createAction(CLEAR_IMPORT_GAME);
export const cancelLookupGame = createAction(CANCEL_LOOKUP_GAME);

export const setImportGameValue = createAction(
  SET_IMPORT_GAME_VALUE,
  (payload: SetValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [QUEUE_LOOKUP_GAME]: function (
    getState: () => AppState,
    payload: QueuePayload,
    dispatch: AppDispatch
  ) {
    const { name, path, relativePath, term, topOfQueue = false } = payload;

    const state = getState().importGame as ImportGameAppState;
    const item: ImportGameItem = _.find(state.items, { id: name }) || {
      id: name,
      term,
      path,
      relativePath,
      isFetching: false,
      isPopulated: false,
      error: null,
      isQueued: false,
      items: [],
    };

    dispatch(
      updateItem({
        section,
        ...item,
        term,
        isQueued: true,
        items: [],
      })
    );

    const itemIndex = queue.indexOf(item.id);

    if (itemIndex >= 0) {
      queue.splice(itemIndex, 1);
    }

    if (topOfQueue) {
      queue.unshift(item.id);
    } else {
      queue.push(item.id);
    }

    if (term && term.length > 2) {
      dispatch(startLookupGame({ start: true }));
    }
  },

  [START_LOOKUP_GAME]: function (
    getState: () => AppState,
    payload: StartPayload,
    dispatch: AppDispatch
  ) {
    if (concurrentLookups >= 1) {
      return;
    }

    const state = getState().importGame as ImportGameAppState;

    const { isLookingUpGame, items } = state;

    const queueId = queue[0];

    if (payload.start && !isLookingUpGame) {
      dispatch(set({ section, isLookingUpGame: true }));
    } else if (!isLookingUpGame) {
      return;
    } else if (!queueId) {
      dispatch(set({ section, isLookingUpGame: false }));
      return;
    }

    concurrentLookups++;
    queue.splice(0, 1);

    const queued = items.find((i) => i.id === queueId);

    if (!queued) {
      concurrentLookups--;
      dispatch(startLookupGame({}));
      return;
    }

    dispatch(
      updateItem({
        section,
        id: queued.id,
        isFetching: true,
      })
    );

    const { request, abortRequest } = createAjaxRequest({
      url: '/game/lookup',
      data: {
        term: queued.term,
      },
    });

    abortCurrentLookup = abortRequest;

    request.done((data: SelectedGame[]) => {
      const selectedGame = queued.selectedGame || data[0];

      dispatch(
        updateItem({
          section,
          id: queued.id,
          isFetching: false,
          isPopulated: true,
          error: null,
          items: data,
          isQueued: false,
          selectedGame,
          updateOnly: true,
        })
      );
    });

    request.fail((xhr: unknown) => {
      dispatch(
        updateItem({
          section,
          id: queued.id,
          isFetching: false,
          isPopulated: false,
          error: xhr,
          isQueued: false,
          updateOnly: true,
        })
      );
    });

    request.always(() => {
      concurrentLookups--;

      dispatch(startLookupGame({}));
    });
  },

  [LOOKUP_UNSEARCHED_GAMES]: function (
    getState: () => AppState,
    _payload: unknown,
    dispatch: AppDispatch
  ) {
    const state = getState().importGame as ImportGameAppState;

    if (state.isLookingUpGame) {
      return;
    }

    state.items.forEach((item) => {
      const id = item.id;

      if (!item.isPopulated && !queue.includes(id)) {
        queue.push(item.id);
      }
    });

    if (queue.length) {
      dispatch(startLookupGame({ start: true }));
    }
  },

  [IMPORT_GAME]: function (
    getState: () => AppState,
    payload: ImportPayload,
    dispatch: AppDispatch
  ) {
    dispatch(set({ section, isImporting: true }));

    const ids = payload.ids;
    const items = (getState().importGame as ImportGameAppState).items;
    const addedIds: string[] = [];

    const allNewGames = ids.reduce((acc: SelectedGame[], id) => {
      const item = items.find((i) => i.id === id);

      if (!item) {
        return acc;
      }

      const selectedGame = item.selectedGame;

      // Make sure we have a selected game and
      // the same game hasn't been added yet.
      if (selectedGame && !acc.some((a) => a.igdbId === selectedGame.igdbId)) {
        // selectedGame comes from the lookup API and contains game data
        // item contains the import options (monitor, qualityProfileId, rootFolderPath, etc.)
        const newGame = getNewGame(
          _.cloneDeep(selectedGame) as Game,
          item as NewGamePayload
        );
        newGame.path = item.path;

        addedIds.push(id);
        acc.push(newGame);
      }

      return acc;
    }, []);

    const promise = createAjaxRequest({
      url: '/game/import',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(allNewGames),
    }).request;

    promise.done((data: Array<{ id: number; [key: string]: unknown }>) => {
      dispatch(
        batchActions([
          set({
            section,
            isImporting: false,
            isImported: true,
            importError: null,
          }),

          ...data.map((game) => updateItem({ section: 'games', ...game })),

          ...addedIds.map((id) => removeItem({ section, id })),
        ])
      );

      dispatch(fetchRootFolders());
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        batchActions([
          set({
            section,
            isImporting: false,
            isImported: true,
            importError: xhr,
          }),

          ...addedIds.map((id) =>
            updateItem({
              section,
              id,
            })
          ),
        ])
      );
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [CANCEL_LOOKUP_GAME]: function (state: ImportGameAppState) {
      queue.splice(0, queue.length);

      const items = state.items.map((item) => {
        if (item.isQueued) {
          return {
            ...item,
            isQueued: false,
          };
        }

        return item;
      });

      return Object.assign({}, state, {
        isLookingUpGame: false,
        items,
      });
    },

    [CLEAR_IMPORT_GAME]: function (state: ImportGameAppState) {
      if (abortCurrentLookup) {
        abortCurrentLookup();

        abortCurrentLookup = null;
      }

      queue.splice(0, queue.length);

      return Object.assign({}, state, defaultState);
    },

    [SET_IMPORT_GAME_VALUE]: function (
      state: object,
      { payload }: { payload: SetValuePayload }
    ) {
      const newState = getSectionState<ImportGameAppState>(state, section);
      const items = newState.items;
      const index = items.findIndex((item) => item.id === payload.id);

      newState.items = [...items];

      if (index >= 0) {
        const item = items[index];

        newState.items.splice(index, 1, { ...item, ...payload });
      } else {
        // Provide defaults for required ImportGameItem fields
        // payload.id is used in the spread, so we don't need to specify it separately
        const newItem: ImportGameItem = {
          term: '',
          path: '',
          relativePath: '',
          isFetching: false,
          isPopulated: false,
          isQueued: false,
          error: null,
          items: [],
          ...payload,
        };
        newState.items.push(newItem);
      }

      return updateSectionState(state, section, newState);
    },
  },
  defaultState,
  section
);
