import _ from 'lodash';
import moment from 'moment/moment';
import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import {
  filterBuilderTypes,
  filterBuilderValueTypes,
  filterTypes,
  sortDirections,
} from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import sortByProp from 'Utilities/Array/sortByProp';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getNewGame, { NewGamePayload } from 'Utilities/Game/getNewGame';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';
import { removeItem, set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createClearReducer from './Creators/Reducers/createClearReducer';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';
import { filterPredicates } from './gameActions';

//
// Variables

export const section = 'discoverGame';

//
// State

interface DiscoverGameDefaults {
  rootFolderPath: string;
  monitor: string;
  qualityProfileId: number;
  minimumAvailability: string;
  searchForGame: boolean;
  tags: number[];
}

interface DiscoverGameOptions {
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
}

interface PosterOptions {
  size: string;
  showTitle: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
}

interface OverviewOptions {
  size: string;
  showYear: boolean;
  showStudio: boolean;
  showGenres: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  showCertification: boolean;
}

interface Column {
  name: string;
  label?: () => string;
  columnLabel?: () => string;
  isSortable?: boolean;
  isVisible: boolean;
  isModifiable?: boolean;
}

interface FilterItem {
  key: string;
  value?: unknown;
  type?: string;
}

interface Filter {
  key: string;
  label: () => string;
  filters: FilterItem[];
}

interface FilterBuilderProp {
  name: string;
  label: () => string;
  type: string;
  valueType?: string;
  optionsSelector?: (
    items: DiscoverGameItem[]
  ) => Array<{ id: string; name: string }>;
}

interface DiscoverGameItem {
  id: number;
  igdbId: number;
  title: string;
  sortTitle: string;
  year?: number;
  studio?: string;
  status: string;
  genres: string[];
  collection?: { title?: string; igdbId?: number };
  originalLanguage?: { name: string };
  inCinemas?: string;
  physicalRelease?: string;
  digitalRelease?: string;
  ratings?: {
    igdb?: { value: number };
    metacritic?: { value: number };
  };
  isExcluded: boolean;
  isExisting: boolean;
  lists: unknown[];
}

export interface DiscoverGameState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isAdding: boolean;
  isAdded: boolean;
  addError: unknown;
  isSaving?: boolean;
  saveError?: unknown;
  items: DiscoverGameItem[];
  sortKey: string;
  sortDirection: string;
  secondarySortKey: string;
  secondarySortDirection: string;
  view: string;
  options: DiscoverGameOptions;
  defaults: DiscoverGameDefaults;
  posterOptions: PosterOptions;
  overviewOptions: OverviewOptions;
  tableOptions: Record<string, unknown>;
  columns: Column[];
  sortPredicates: Record<
    string,
    (item: DiscoverGameItem, direction?: string) => unknown
  >;
  selectedFilterKey: string;
  filters: Filter[];
  filterPredicates: typeof filterPredicates;
  filterBuilderProps: FilterBuilderProp[];
}

export const defaultState: DiscoverGameState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isAdding: false,
  isAdded: false,
  addError: null,
  items: [],
  sortKey: 'sortTitle',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortTitle',
  secondarySortDirection: sortDirections.ASCENDING,
  view: 'overview',

  options: {
    includeRecommendations: true,
    includeTrending: true,
    includePopular: true,
  },

  defaults: {
    rootFolderPath: '',
    monitor: 'gameOnly',
    qualityProfileId: 0,
    minimumAvailability: 'released',
    searchForGame: true,
    tags: [],
  },

  posterOptions: {
    size: 'large',
    showTitle: false,
    showIgdbRating: false,
    showMetacriticRating: false,
  },

  overviewOptions: {
    size: 'medium',
    showYear: true,
    showStudio: true,
    showGenres: true,
    showIgdbRating: false,
    showMetacriticRating: false,
    showCertification: true,
  },

  tableOptions: {
    // showSearchAction: false
  },

  columns: [
    {
      name: 'status',
      columnLabel: () => translate('Status'),
      isSortable: true,
      isVisible: true,
      isModifiable: false,
    },
    {
      name: 'isRecommendation',
      columnLabel: () => translate('Recommendation'),
      isSortable: true,
      isVisible: true,
      isModifiable: false,
    },
    {
      name: 'isTrending',
      columnLabel: () => translate('Trending'),
      isSortable: true,
      isVisible: true,
      isModifiable: false,
    },
    {
      name: 'isPopular',
      columnLabel: () => translate('Popular'),
      isSortable: true,
      isVisible: true,
      isModifiable: false,
    },
    {
      name: 'sortTitle',
      label: () => translate('GameTitle'),
      isSortable: true,
      isVisible: true,
      isModifiable: false,
    },
    {
      name: 'originalLanguage',
      label: () => translate('OriginalLanguage'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'collection',
      label: () => translate('Collection'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'studio',
      label: () => translate('Studio'),
      isSortable: true,
      isVisible: true,
    },
    {
      name: 'year',
      label: () => translate('Year'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'inCinemas',
      label: () => translate('InDevelopment'),
      isSortable: true,
      isVisible: true,
    },
    {
      name: 'digitalRelease',
      label: () => translate('DigitalRelease'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'physicalRelease',
      label: () => translate('PhysicalRelease'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'runtime',
      label: () => translate('Runtime'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'genres',
      label: () => translate('Genres'),
      isSortable: false,
      isVisible: false,
    },
    {
      name: 'igdbRating',
      label: () => translate('IgdbRating'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'metacriticRating',
      label: () => translate('MetacriticRating'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'popularity',
      label: () => translate('Popularity'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'certification',
      label: () => translate('Certification'),
      isSortable: true,
      isVisible: false,
    },
    {
      name: 'lists',
      label: () => translate('Lists'),
      isSortable: false,
      isVisible: false,
    },
    {
      name: 'actions',
      columnLabel: () => translate('Actions'),
      isVisible: true,
      isModifiable: false,
    },
  ],

  sortPredicates: {
    status: function (item: DiscoverGameItem) {
      let result = 0;

      if (item.isExcluded) {
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

    collection: function (item: DiscoverGameItem) {
      const { collection = {} } = item;

      return collection.title;
    },

    originalLanguage: function (item: DiscoverGameItem) {
      const { originalLanguage = {} as { name?: string } } = item;

      return originalLanguage.name;
    },

    studio: function (item: DiscoverGameItem) {
      const studio = item.studio;

      return studio ? studio.toLowerCase() : '';
    },

    inCinemas: function (item: DiscoverGameItem, direction?: string) {
      if (item.inCinemas) {
        return moment(item.inCinemas).unix();
      }

      if (direction === sortDirections.DESCENDING) {
        return -1 * Number.MAX_VALUE;
      }

      return Number.MAX_VALUE;
    },

    physicalRelease: function (item: DiscoverGameItem, direction?: string) {
      if (item.physicalRelease) {
        return moment(item.physicalRelease).unix();
      }

      if (direction === sortDirections.DESCENDING) {
        return -1 * Number.MAX_VALUE;
      }

      return Number.MAX_VALUE;
    },

    digitalRelease: function (item: DiscoverGameItem, direction?: string) {
      if (item.digitalRelease) {
        return moment(item.digitalRelease).unix();
      }

      if (direction === sortDirections.DESCENDING) {
        return -1 * Number.MAX_VALUE;
      }

      return Number.MAX_VALUE;
    },

    igdbRating: function ({ ratings = {} }: DiscoverGameItem) {
      return ratings.igdb ? ratings.igdb.value : 0;
    },

    metacriticRating: function ({ ratings = {} }: DiscoverGameItem) {
      return ratings.metacritic ? ratings.metacritic.value : 0;
    },
  },

  selectedFilterKey: 'newNotExcluded',

  filters: [
    {
      key: 'all',
      label: () => translate('All'),
      filters: [],
    },
    {
      key: 'popular',
      label: () => translate('Popular'),
      filters: [
        {
          key: 'isPopular',
          value: true,
          type: filterTypes.EQUAL,
        },
      ],
    },
    {
      key: 'trending',
      label: () => translate('Trending'),
      filters: [
        {
          key: 'isTrending',
          value: true,
          type: filterTypes.EQUAL,
        },
      ],
    },
    {
      key: 'newNotExcluded',
      label: () => translate('NewNonExcluded'),
      filters: [
        {
          key: 'isExisting',
          value: false,
          type: filterTypes.EQUAL,
        },
        {
          key: 'isExcluded',
          value: false,
          type: filterTypes.EQUAL,
        },
      ],
    },
  ],

  filterPredicates,

  filterBuilderProps: [
    {
      name: 'status',
      label: () => translate('ReleaseStatus'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.RELEASE_STATUS,
    },
    {
      name: 'studio',
      label: () => translate('Studio'),
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function (items: DiscoverGameItem[]) {
        const tagList = items.reduce(
          (acc: Array<{ id: string; name: string }>, game) => {
            acc.push({
              id: game.studio || '',
              name: game.studio || '',
            });

            return acc;
          },
          []
        );

        return tagList.sort(sortByProp('name'));
      },
    },
    {
      name: 'collection',
      label: () => translate('Collection'),
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function (items: DiscoverGameItem[]) {
        const collectionList = items.reduce(
          (acc: Array<{ id: string; name: string }>, game) => {
            if (game.collection && game.collection.title) {
              acc.push({
                id: game.collection.title,
                name: game.collection.title,
              });
            }

            return acc;
          },
          []
        );

        return collectionList.sort(sortByProp('name'));
      },
    },
    {
      name: 'originalLanguage',
      label: () => translate('OriginalLanguage'),
      type: filterBuilderTypes.EXACT,
      optionsSelector: function (items: DiscoverGameItem[]) {
        const collectionList = items.reduce(
          (acc: Array<{ id: string; name: string }>, game) => {
            if (game.originalLanguage) {
              acc.push({
                id: game.originalLanguage.name,
                name: game.originalLanguage.name,
              });
            }

            return acc;
          },
          []
        );

        return collectionList.sort(sortByProp('name'));
      },
    },
    {
      name: 'inCinemas',
      label: () => translate('InDevelopment'),
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE,
    },
    {
      name: 'physicalRelease',
      label: () => translate('PhysicalRelease'),
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE,
    },
    {
      name: 'digitalRelease',
      label: () => translate('DigitalRelease'),
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE,
    },
    {
      name: 'runtime',
      label: () => translate('Runtime'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'genres',
      label: () => translate('Genres'),
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function (items: DiscoverGameItem[]) {
        const tagList = items.reduce(
          (acc: Array<{ id: string; name: string }>, game) => {
            game.genres.forEach((genre) => {
              acc.push({
                id: genre,
                name: genre,
              });
            });

            return acc;
          },
          []
        );

        return tagList.sort(sortByProp('name'));
      },
    },
    {
      name: 'isAvailable',
      label: () => translate('ConsideredAvailable'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'minimumAvailability',
      label: () => translate('MinimumAvailability'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.MINIMUM_AVAILABILITY,
    },
    {
      name: 'igdbRating',
      label: () => translate('IgdbRating'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'igdbVotes',
      label: () => translate('IgdbVotes'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'metacriticRating',
      label: () => translate('MetacriticRating'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'popularity',
      label: () => translate('Popularity'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'certification',
      label: () => translate('Certification'),
      type: filterBuilderTypes.EXACT,
    },
    {
      name: 'lists',
      label: () => translate('Lists'),
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.IMPORTLIST,
    },
    {
      name: 'isExcluded',
      label: () => translate('OnExcludedList'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'isExisting',
      label: () => translate('ExistsInLibrary'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'isRecommendation',
      label: () => translate('Recommended'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'isTrending',
      label: () => translate('Trending'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'isPopular',
      label: () => translate('Popular'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
  ],
};

export const persistState = [
  'discoverGame.defaults',
  'discoverGame.sortKey',
  'discoverGame.sortDirection',
  'discoverGame.selectedFilterKey',
  'discoverGame.customFilters',
  'discoverGame.view',
  'discoverGame.columns',
  'discoverGame.options',
  'discoverGame.posterOptions',
  'discoverGame.overviewOptions',
  'discoverGame.tableOptions',
];

//
// Actions Types

export const ADD_GAME = 'discoverGame/addGame';
export const ADD_GAMES = 'discoverGame/addGames';
export const SET_ADD_GAME_VALUE = 'discoverGame/setAddGameValue';
export const CLEAR_ADD_GAME = 'discoverGame/clearAddGame';
export const SET_ADD_GAME_DEFAULT = 'discoverGame/setAddGameDefault';

export const FETCH_DISCOVER_GAMES = 'discoverGame/fetchDiscoverGames';

export const SET_LIST_GAME_SORT = 'discoverGame/setListGameSort';
export const SET_LIST_GAME_FILTER = 'discoverGame/setListGameFilter';
export const SET_LIST_GAME_VIEW = 'discoverGame/setListGameView';
export const SET_LIST_GAME_OPTION = 'discoverGame/setListGameGameOption';
export const SET_LIST_GAME_TABLE_OPTION = 'discoverGame/setListGameTableOption';
export const SET_LIST_GAME_POSTER_OPTION =
  'discoverGame/setListGamePosterOption';
export const SET_LIST_GAME_OVERVIEW_OPTION =
  'discoverGame/setListGameOverviewOption';

export const ADD_IMPORT_LIST_EXCLUSIONS =
  'discoverGame/addImportListExclusions';

//
// Action Creators

interface AddGamePayload extends NewGamePayload {
  igdbId: number;
}

interface AddGamesPayload {
  ids: number[];
  addOptions: NewGamePayload;
}

interface AddImportListExclusionsPayload {
  ids: number[];
}

interface SetAddGameValuePayload {
  [key: string]: unknown;
}

interface SetListGameViewPayload {
  view: string;
}

export const addGame = createThunk(ADD_GAME);
export const addGames = createThunk(ADD_GAMES);
export const clearAddGame = createAction(CLEAR_ADD_GAME);
export const setAddGameDefault =
  createAction<Partial<DiscoverGameDefaults>>(SET_ADD_GAME_DEFAULT);

export const fetchDiscoverGames = createThunk(FETCH_DISCOVER_GAMES);

export const setListGameSort = createAction(SET_LIST_GAME_SORT);
export const setListGameFilter = createAction(SET_LIST_GAME_FILTER);
export const setListGameView =
  createAction<SetListGameViewPayload>(SET_LIST_GAME_VIEW);
export const setListGameOption =
  createAction<Partial<DiscoverGameOptions>>(SET_LIST_GAME_OPTION);
export const setListGameTableOption = createAction(SET_LIST_GAME_TABLE_OPTION);
export const setListGamePosterOption = createAction<Partial<PosterOptions>>(
  SET_LIST_GAME_POSTER_OPTION
);
export const setListGameOverviewOption = createAction<Partial<OverviewOptions>>(
  SET_LIST_GAME_OVERVIEW_OPTION
);

export const addImportListExclusions = createThunk(ADD_IMPORT_LIST_EXCLUSIONS);

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
  [FETCH_DISCOVER_GAMES]: function (
    getState: () => AppState,
    payload: { id?: number },
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isFetching: true }));

    const { id, ...otherPayload } = payload;

    const state = getState();
    const {
      includeRecommendations = false,
      includeTrending = false,
      includePopular = false,
    } = state.discoverGame.options;

    const promise = createAjaxRequest({
      url: `/importlist/game?includeRecommendations=${includeRecommendations}&includeTrending=${includeTrending}&includePopular=${includePopular}`,
      data: otherPayload,
      traditional: true,
    }).request;

    promise.done((data: Array<{ igdbId: number; [key: string]: unknown }>) => {
      // set an ID so the selectors and updaters done blow up.
      const mappedData = data.map((game) => ({ ...game, id: game.igdbId }));

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
    });

    promise.fail((xhr: { aborted?: boolean }) => {
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
    const state = getState();
    const items = state.discoverGame.items;
    const itemToUpdate = _.find(items, { igdbId });

    if (!itemToUpdate) {
      dispatch(set({ section, isAdding: false }));
      return;
    }

    const newGame = getNewGame(_.cloneDeep(itemToUpdate), payload);
    (newGame as { id: number }).id = 0;

    const promise = createAjaxRequest({
      url: '/game',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(newGame),
    }).request;

    promise.done((data: unknown) => {
      dispatch(
        batchActions([
          updateItem({
            section: 'games',
            ...(data as Record<string, unknown>),
          }),

          itemToUpdate.lists.length === 0
            ? removeItem({ section: 'discoverGame', ...itemToUpdate })
            : updateItem({
                section: 'discoverGame',
                ...itemToUpdate,
                isExisting: true,
              }),

          set({
            section,
            isAdding: false,
            isAdded: true,
            addError: null,
          }),
        ])
      );
    });

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

  [ADD_GAMES]: function (
    getState: () => AppState,
    payload: AddGamesPayload,
    dispatch: Dispatch
  ) {
    dispatch(set({ section, isAdding: true }));

    const ids = payload.ids;
    const addOptions = payload.addOptions;
    const state = getState();
    const items = state.discoverGame.items;
    const addedIds: number[] = [];

    const allNewGames = ids.reduce(
      (acc: Array<{ id: number; igdbId: number }>, id) => {
        const item = items.find((i) => i.id === id);
        const selectedGame = item;

        // Make sure we have a selected game and
        // the same game hasn't been added yet.
        if (
          selectedGame &&
          !acc.some((a) => a.igdbId === selectedGame.igdbId)
        ) {
          if (!selectedGame.isExisting) {
            const newGame = getNewGame(_.cloneDeep(selectedGame), addOptions);
            (newGame as { id: number }).id = 0;

            addedIds.push(id);
            acc.push(newGame);
          }
        }

        return acc;
      },
      []
    );

    const promise = createAjaxRequest({
      url: '/importlist/game',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(allNewGames),
    }).request;

    promise.done((data: unknown[]) => {
      dispatch(
        batchActions([
          set({
            section,
            isAdding: false,
            isAdded: true,
          }),

          ...data.map((game) =>
            updateItem({
              section: 'games',
              ...(game as Record<string, unknown>),
            })
          ),

          ...addedIds.map((id) =>
            items.find((i) => i.id === id)!.lists.length === 0
              ? removeItem({ section, id })
              : updateItem({ section, id, isExisting: true })
          ),
        ])
      );
    });

    promise.fail(() => {
      dispatch(
        set({
          section,
          isAdding: false,
          isAdded: true,
        })
      );
    });
  },

  [ADD_IMPORT_LIST_EXCLUSIONS]: function (
    getState: () => AppState,
    payload: AddImportListExclusionsPayload,
    dispatch: Dispatch
  ) {
    const ids = payload.ids;
    const state = getState();
    const items = state.discoverGame.items;

    const exclusions = ids.reduce(
      (
        acc: Array<{ igdbId: number; gameTitle: string; gameYear?: number }>,
        id
      ) => {
        const item = items.find((i) => i.igdbId === id);

        if (item) {
          const newExclusion = {
            igdbId: id,
            gameTitle: item.title,
            gameYear: item.year,
          };

          acc.push(newExclusion);
        }

        return acc;
      },
      []
    );

    const promise = createAjaxRequest({
      url: '/exclusions/bulk',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(exclusions),
    }).request;

    promise.done((data: Array<{ igdbId: number; [key: string]: unknown }>) => {
      dispatch(
        batchActions([
          ...data.map((item) =>
            updateItem({ section: 'settings.importListExclusions', ...item })
          ),

          ...data.map((item) =>
            updateItem({ section, id: item.igdbId, isExcluded: true })
          ),

          set({
            section,
            isSaving: false,
            saveError: null,
          }),
        ])
      );
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        set({
          section,
          isSaving: false,
          saveError: xhr,
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
      state: DiscoverGameState,
      { payload }: { payload: Partial<DiscoverGameDefaults> }
    ) {
      const newState = getSectionState<{
        defaults: DiscoverGameDefaults;
      }>(state, section);

      newState.defaults = {
        ...newState.defaults,
        ...payload,
      };

      return updateSectionState(state, section, newState);
    },

    [SET_LIST_GAME_SORT]: createSetClientSideCollectionSortReducer(section),
    [SET_LIST_GAME_FILTER]: createSetClientSideCollectionFilterReducer(section),

    [SET_LIST_GAME_VIEW]: function (
      state: DiscoverGameState,
      { payload }: { payload: SetListGameViewPayload }
    ) {
      return Object.assign({}, state, { view: payload.view });
    },

    [SET_LIST_GAME_OPTION]: function (
      state: DiscoverGameState,
      { payload }: { payload: Partial<DiscoverGameOptions> }
    ) {
      const discoveryGameOptions = state.options;

      return {
        ...state,
        options: {
          ...discoveryGameOptions,
          ...payload,
        },
      };
    },

    [SET_LIST_GAME_TABLE_OPTION]: createSetTableOptionReducer(section),

    [SET_LIST_GAME_POSTER_OPTION]: function (
      state: DiscoverGameState,
      { payload }: { payload: Partial<PosterOptions> }
    ) {
      const posterOptions = state.posterOptions;

      return {
        ...state,
        posterOptions: {
          ...posterOptions,
          ...payload,
        },
      };
    },

    [SET_LIST_GAME_OVERVIEW_OPTION]: function (
      state: DiscoverGameState,
      { payload }: { payload: Partial<OverviewOptions> }
    ) {
      const overviewOptions = state.overviewOptions;

      return {
        ...state,
        overviewOptions: {
          ...overviewOptions,
          ...payload,
        },
      };
    },

    [CLEAR_ADD_GAME]: createClearReducer(section, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: [],
    }),
  },
  defaultState,
  section
);
