import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes, filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import sortByProp from 'Utilities/Array/sortByProp';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getNewGame from 'Utilities/Game/getNewGame';
import translate from 'Utilities/String/translate';
import { set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'gameCollections';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],
  isSaving: false,
  saveError: null,
  isAdding: false,
  addError: null,
  sortKey: 'sortTitle',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortTitle',
  secondarySortDirection: sortDirections.ASCENDING,
  view: 'overview',
  pendingChanges: {},

  overviewOptions: {
    detailedProgressBar: false,
    size: 'medium',
    showDetails: true,
    showOverview: true,
    showPosters: true
  },

  defaults: {
    rootFolderPath: '',
    monitor: 'gameOnly',
    qualityProfileId: 0,
    minimumAvailability: 'released',
    searchForGame: true,
    tags: []
  },

  selectedFilterKey: 'all',

  filters: [
    {
      key: 'all',
      label: () => translate('All'),
      filters: []
    },
    {
      key: 'missing',
      label: () => translate('Missing'),
      filters: [
        {
          key: 'missingGames',
          value: 0,
          type: filterTypes.GREATER_THAN
        }
      ]
    },
    {
      key: 'complete',
      label: () => translate('Complete'),
      filters: [
        {
          key: 'missingGames',
          value: 0,
          type: filterTypes.EQUAL
        }
      ]
    }
  ],

  filterPredicates: {
    genres: function(item, filterValue, type) {
      const predicate = filterTypePredicates[type];

      const allGenres = item.games.flatMap(({ genres }) => genres);
      const genres = Array.from(new Set(allGenres));

      return predicate(genres, filterValue);
    },
    totalGames: function(item, filterValue, type) {
      const predicate = filterTypePredicates[type];
      const { games } = item;

      const totalGames = games.length;
      return predicate(totalGames, filterValue);
    }
  },

  filterBuilderProps: [
    {
      name: 'title',
      label: () => translate('Title'),
      type: filterBuilderTypes.STRING
    },
    {
      name: 'monitored',
      label: () => translate('Monitored'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'qualityProfileId',
      label: () => translate('QualityProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'rootFolderPath',
      label: () => translate('RootFolder'),
      type: filterBuilderTypes.STRING
    },
    {
      name: 'genres',
      label: () => translate('Genres'),
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function(items) {
        const genreList = items.reduce((acc, collection) => {
          const collectionGenres = collection.games.flatMap(({ genres }) => genres);
          const genres = Array.from(new Set(collectionGenres));

          genres.forEach((genre) => {
            acc.push({
              id: genre,
              name: genre
            });
          });

          return acc;
        }, []);

        return genreList.sort(sortByProp('name'));
      }
    },
    {
      name: 'totalGames',
      label: () => translate('TotalGames'),
      type: filterBuilderTypes.NUMBER
    }
  ]
};

export const persistState = [
  'gameCollections.defaults',
  'gameCollections.sortKey',
  'gameCollections.sortDirection',
  'gameCollections.selectedFilterKey',
  'gameCollections.customFilters',
  'gameCollections.options',
  'gameCollections.overviewOptions'
];

//
// Actions Types

export const FETCH_GAME_COLLECTIONS = 'gameCollections/fetchGameCollections';
export const CLEAR_GAME_COLLECTIONS = 'gameCollections/clearGameCollections';
export const SAVE_GAME_COLLECTION = 'gameCollections/saveGameCollection';
export const SAVE_GAME_COLLECTIONS = 'gameCollections/saveGameCollections';
export const SET_GAME_COLLECTION_VALUE = 'gameCollections/setGameCollectionValue';

export const ADD_GAME = 'gameCollections/addGame';

export const TOGGLE_COLLECTION_MONITORED = 'gameCollections/toggleCollectionMonitored';

export const SET_GAME_COLLECTIONS_SORT = 'gameCollections/setGameCollectionsSort';
export const SET_GAME_COLLECTIONS_FILTER = 'gameCollections/setGameCollectionsFilter';
export const SET_GAME_COLLECTIONS_OPTION = 'gameCollections/setGameCollectionsOption';
export const SET_GAME_COLLECTIONS_OVERVIEW_OPTION = 'gameCollections/setGameCollectionsOverviewOption';

//
// Action Creators

export const fetchGameCollections = createThunk(FETCH_GAME_COLLECTIONS);
export const clearGameCollections = createAction(CLEAR_GAME_COLLECTIONS);
export const saveGameCollection = createThunk(SAVE_GAME_COLLECTION);
export const saveGameCollections = createThunk(SAVE_GAME_COLLECTIONS);

export const addGame = createThunk(ADD_GAME);

export const toggleCollectionMonitored = createThunk(TOGGLE_COLLECTION_MONITORED);

export const setGameCollectionsSort = createAction(SET_GAME_COLLECTIONS_SORT);
export const setGameCollectionsFilter = createAction(SET_GAME_COLLECTIONS_FILTER);
export const setGameCollectionsOption = createAction(SET_GAME_COLLECTIONS_OPTION);
export const setGameCollectionsOverviewOption = createAction(SET_GAME_COLLECTIONS_OVERVIEW_OPTION);

export const setGameCollectionValue = createAction(SET_GAME_COLLECTION_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Action Handlers

export const actionHandlers = handleThunks({

  [SAVE_GAME_COLLECTION]: createSaveProviderHandler(section, '/collection'),
  [FETCH_GAME_COLLECTIONS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/collection',
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
  },

  [ADD_GAME]: function(getState, payload, dispatch) {
    dispatch(set({ section, isAdding: true }));

    const { igdbId, title } = payload;

    const newGame = getNewGame({ igdbId, title }, payload);
    newGame.id = 0;

    const promise = createAjaxRequest({
      url: '/game',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(newGame)
    }).request;

    promise.done((data) => {
      const collectionToUpdate = getState().gameCollections.items.find((collection) => collection.igdbId === data.collection.igdbId);
      const collectionData = { ...collectionToUpdate, missingGames: Math.max(0, collectionToUpdate.missingGames - 1 ) };

      dispatch(batchActions([
        updateItem({ section: 'games', ...data }),

        set({
          section,
          isAdding: false,
          isAdded: true,
          addError: null
        }),

        updateItem({ section, ...collectionData })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        isAdded: false,
        addError: xhr
      }));
    });
  },

  [TOGGLE_COLLECTION_MONITORED]: (getState, payload, dispatch) => {
    const {
      collectionId: id,
      monitored
    } = payload;

    const collection = _.find(getState().gameCollections.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/collection/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...collection,
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

  [SAVE_GAME_COLLECTIONS]: function(getState, payload, dispatch) {
    const {
      collectionIds,
      monitored,
      monitor,
      qualityProfileId,
      minimumAvailability,
      rootFolderPath,
      searchOnAdd
    } = payload;

    const response = {};

    if (payload.hasOwnProperty('monitored')) {
      response.monitored = monitored;
    }

    if (payload.hasOwnProperty('monitor')) {
      response.monitorGames = monitor === 'monitored';
    }

    if (payload.hasOwnProperty('qualityProfileId')) {
      response.qualityProfileId = qualityProfileId;
    }

    if (payload.hasOwnProperty('minimumAvailability')) {
      response.minimumAvailability = minimumAvailability;
    }

    if (payload.hasOwnProperty('searchOnAdd')) {
      response.searchOnAdd = searchOnAdd;
    }

    response.rootFolderPath = rootFolderPath;
    response.collectionIds = collectionIds;

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/collection',
      method: 'PUT',
      data: JSON.stringify(response),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(fetchGameCollections());

      dispatch(set({
        section,
        isSaving: false,
        saveError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_GAME_COLLECTIONS_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_GAME_COLLECTIONS_FILTER]: createSetClientSideCollectionFilterReducer(section),
  [SET_GAME_COLLECTION_VALUE]: createSetSettingValueReducer(section),

  [SET_GAME_COLLECTIONS_OPTION]: function(state, { payload }) {
    const gameCollectionsOptions = state.options;

    return {
      ...state,
      options: {
        ...gameCollectionsOptions,
        ...payload
      }
    };
  },

  [SET_GAME_COLLECTIONS_OVERVIEW_OPTION]: function(state, { payload }) {
    const overviewOptions = state.overviewOptions;

    return {
      ...state,
      overviewOptions: {
        ...overviewOptions,
        ...payload
      }
    };
  },

  [CLEAR_GAME_COLLECTIONS]: (state) => {
    return Object.assign({}, state, defaultState);
  }

}, defaultState, section);
