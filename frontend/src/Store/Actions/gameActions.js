import _ from 'lodash';
import moment from 'moment';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
// import { batchActions } from 'redux-batched-actions';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import padNumber from 'Utilities/Number/padNumber';
import translate from 'Utilities/String/translate';
import { set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'games';

export const filters = [
  {
    key: 'all',
    label: () => translate('All'),
    filters: []
  },
  {
    key: 'monitored',
    label: () => translate('MonitoredOnly'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'unmonitored',
    label: () => translate('Unmonitored'),
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: () => translate('Missing'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'hasFile',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'wanted',
    label: () => translate('Wanted'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'hasFile',
        value: false,
        type: filterTypes.EQUAL
      },
      {
        key: 'isAvailable',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'cutoffunmet',
    label: () => translate('CutoffUnmet'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'hasFile',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'qualityCutoffNotMet',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  }
];

export const filterPredicates = {
  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  collection: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const { collection } = item;

    return predicate(collection && collection.title ? collection.title : '', filterValue);
  },

  originalLanguage: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const { originalLanguage } = item;

    return predicate(originalLanguage ? originalLanguage.name : '', filterValue);
  },

  releaseGroups: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const { statistics = {} } = item;
    const { releaseGroups = [] } = statistics;

    return predicate(releaseGroups, filterValue);
  },

  sizeOnDisk: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const { statistics = {} } = item;
    const sizeOnDisk = statistics && statistics.sizeOnDisk ? statistics.sizeOnDisk : 0;

    return predicate(sizeOnDisk, filterValue);
  },

  inCinemas: function(item, filterValue, type) {
    return dateFilterPredicate(item.inCinemas, filterValue, type);
  },

  physicalRelease: function(item, filterValue, type) {
    return dateFilterPredicate(item.physicalRelease, filterValue, type);
  },

  digitalRelease: function(item, filterValue, type) {
    return dateFilterPredicate(item.digitalRelease, filterValue, type);
  },

  releaseDate: function(item, filterValue, type) {
    return dateFilterPredicate(item.releaseDate, filterValue, type);
  },

  igdbRating: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.igdb ? ratings.igdb.value : 0;

    return predicate(rating * 10, filterValue);
  },

  igdbVotes: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.igdb ? ratings.igdb.votes : 0;

    return predicate(rating, filterValue);
  },

  imdbRating: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.imdb ? ratings.imdb.value : 0;

    return predicate(rating, filterValue);
  },

  imdbVotes: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.imdb ? ratings.imdb.votes : 0;

    return predicate(rating, filterValue);
  },

  rottenTomatoesRating: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.rottenTomatoes ? ratings.rottenTomatoes.value : 0;

    return predicate(rating, filterValue);
  },

  traktRating: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.trakt ? ratings.trakt.value : 0;

    return predicate(rating * 10, filterValue);
  },

  traktVotes: function({ ratings = {} }, filterValue, type) {
    const predicate = filterTypePredicates[type];

    const rating = ratings.trakt ? ratings.trakt.votes : 0;

    return predicate(rating, filterValue);
  },

  qualityCutoffNotMet: function(item) {
    const { gameFile = {} } = item;

    return gameFile.qualityCutoffNotMet;
  }
};

export const sortPredicates = {
  status: function(item) {
    let result = 0;

    if (item.monitored) {
      result += 4;
    }

    if (item.status === 'announced') {
      result++;
    }

    if (item.status === 'inCinemas') {
      result += 2;
    }

    if (item.status === 'released') {
      result += 3;
    }

    return result;
  },

  gameStatus: function(item) {
    let result = 0;
    let qualityName = '';

    const hasGameFile = !!item.gameFile;

    if (item.isAvailable) {
      result++;
    }

    if (item.monitored) {
      result += 2;
    }

    if (hasGameFile) {
      // TODO: Consider Quality Weight for Sorting within status of hasGame
      if (item.gameFile.qualityCutoffNotMet) {
        result += 4;
      } else {
        result += 8;
      }
      qualityName = item.gameFile.quality.quality.name;
    }

    return padNumber(result.toString(), 2) + qualityName;
  },

  year: function(item) {
    return item.year || undefined;
  },

  inCinemas: function(item, direction) {
    const { inCinemas } = item;

    if (inCinemas) {
      return moment(inCinemas).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  physicalRelease: function(item, direction) {
    const { physicalRelease } = item;

    if (physicalRelease) {
      return moment(physicalRelease).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  digitalRelease: function(item, direction) {
    const { digitalRelease } = item;

    if (digitalRelease) {
      return moment(digitalRelease).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  releaseDate: function(item, direction) {
    const { releaseDate } = item;

    if (releaseDate) {
      return moment(releaseDate).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  items: [],
  sortKey: 'sortTitle',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {},
  deleteOptions: {
    addImportExclusion: false
  }
};

export const persistState = [
  'games.deleteOptions'
];

//
// Actions Types

export const FETCH_GAMES = 'games/fetchGames';
export const SET_GAME_VALUE = 'games/setGameValue';
export const SAVE_GAME = 'games/saveGame';
export const DELETE_GAME = 'games/deleteGame';
export const SAVE_GAME_EDITOR = 'games/saveGameEditor';
export const BULK_DELETE_GAME = 'games/bulkDeleteGame';

export const SET_DELETE_OPTION = 'games/setDeleteOption';

export const TOGGLE_GAME_MONITORED = 'games/toggleGameMonitored';

//
// Action Creators

export const fetchGames = createThunk(FETCH_GAMES);
export const saveGame = createThunk(SAVE_GAME, (payload) => {
  const newPayload = {
    ...payload
  };

  if (payload.moveFiles) {
    newPayload.queryParams = {
      moveFiles: true
    };
  }

  delete newPayload.moveFiles;

  return newPayload;
});

export const deleteGame = createThunk(DELETE_GAME, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportExclusion: payload.addImportExclusion
    }
  };
});

export const toggleGameMonitored = createThunk(TOGGLE_GAME_MONITORED);
export const saveGameEditor = createThunk(SAVE_GAME_EDITOR);
export const bulkDeleteGame = createThunk(BULK_DELETE_GAME);

export const setGameValue = createAction(SET_GAME_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const setDeleteOption = createAction(SET_DELETE_OPTION);

//
// Helpers

function getSaveAjaxOptions({ ajaxOptions, payload }) {
  if (payload.moveFolder) {
    ajaxOptions.url = `${ajaxOptions.url}?moveFolder=true`;
  }

  return ajaxOptions;
}

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_GAMES]: createFetchHandler(section, '/game'),
  [SAVE_GAME]: createSaveProviderHandler(section, '/game', { getAjaxOptions: getSaveAjaxOptions }),
  [DELETE_GAME]: (getState, payload, dispatch) => {
    createRemoveItemHandler(section, '/game')(getState, payload, dispatch);

    if (!payload.collectionIgdbId) {
      return;
    }

    const collectionToUpdate = getState().gameCollections.items.find((collection) => collection.igdbId === payload.collectionIgdbId);

    // Skip updating if the last game in the collection is being deleted
    if (collectionToUpdate.games.length - collectionToUpdate.missingGames === 1) {
      return;
    }

    const collectionData = { ...collectionToUpdate, missingGames: collectionToUpdate.missingGames + 1 };

    dispatch(updateItem({
      section: 'gameCollections',
      ...collectionData
    }));
  },

  [TOGGLE_GAME_MONITORED]: (getState, payload, dispatch) => {
    const {
      gameId: id,
      monitored
    } = payload;

    const game = _.find(getState().games.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/game/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...game,
        monitored
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false
      }));
    });
  },

  [SAVE_GAME_EDITOR]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/game/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((game) => {
          return updateItem({
            id: game.id,
            section: 'games',
            ...game
          });
        }),

        set({
          section,
          isSaving: false,
          saveError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  },

  [BULK_DELETE_GAME]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isDeleting: true
    }));

    const promise = createAjaxRequest({
      url: '/game/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done(() => {
      // SignaR will take care of removing the game from the collection

      dispatch(set({
        section,
        isDeleting: false,
        deleteError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_GAME_VALUE]: createSetSettingValueReducer(section),
  [SET_DELETE_OPTION]: (state, { payload }) => {
    return {
      ...state,
      deleteOptions: {
        ...payload
      }
    };
  }

}, defaultState, section);
