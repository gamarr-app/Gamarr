import _ from 'lodash';
import moment from 'moment';
import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import {
  filterTypePredicates,
  filterTypes,
  sortDirections,
} from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
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

interface GameItem {
  id: number;
  monitored: boolean;
  status: string;
  isAvailable: boolean;
  gameFile?: {
    qualityCutoffNotMet: boolean;
    quality: {
      quality: {
        name: string;
      };
    };
  };
  year?: number;
  inCinemas?: string;
  physicalRelease?: string;
  digitalRelease?: string;
  releaseDate?: string;
  added?: string;
  collection?: {
    title: string;
  };
  originalLanguage?: {
    name: string;
  };
  statistics?: {
    releaseGroups?: string[];
    sizeOnDisk?: number;
  };
  ratings?: {
    igdb?: {
      value: number;
      votes: number;
    };
    metacritic?: {
      value: number;
    };
  };
}

interface FilterValue {
  key: string;
  value: unknown;
  type: string;
}

interface Filter {
  key: string;
  label: () => string;
  filters: FilterValue[];
}

interface SaveGamePayload {
  moveFiles?: boolean;
  moveFolder?: boolean;
  [key: string]: unknown;
}

interface DeleteGamePayload {
  id: number;
  deleteFiles?: boolean;
  addImportExclusion?: boolean;
  collectionIgdbId?: number;
  [key: string]: unknown;
}

interface ToggleMonitoredPayload {
  gameId: number;
  monitored: boolean;
}


//
// Variables

export const section = 'games';

export const filters: Filter[] = [
  {
    key: 'all',
    label: () => translate('All'),
    filters: [],
  },
  {
    key: 'monitored',
    label: () => translate('MonitoredOnly'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL,
      },
    ],
  },
  {
    key: 'unmonitored',
    label: () => translate('Unmonitored'),
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL,
      },
    ],
  },
  {
    key: 'missing',
    label: () => translate('Missing'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL,
      },
      {
        key: 'hasFile',
        value: false,
        type: filterTypes.EQUAL,
      },
    ],
  },
  {
    key: 'wanted',
    label: () => translate('Wanted'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL,
      },
      {
        key: 'hasFile',
        value: false,
        type: filterTypes.EQUAL,
      },
      {
        key: 'isAvailable',
        value: true,
        type: filterTypes.EQUAL,
      },
    ],
  },
  {
    key: 'cutoffunmet',
    label: () => translate('CutoffUnmet'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL,
      },
      {
        key: 'hasFile',
        value: true,
        type: filterTypes.EQUAL,
      },
      {
        key: 'qualityCutoffNotMet',
        value: true,
        type: filterTypes.EQUAL,
      },
    ],
  },
  {
    key: 'mainGamesOnly',
    label: () => translate('MainGamesOnly'),
    filters: [
      {
        key: 'isDlc',
        value: false,
        type: filterTypes.EQUAL,
      },
    ],
  },
  {
    key: 'dlcOnly',
    label: () => translate('DlcAndExpansions'),
    filters: [
      {
        key: 'isDlc',
        value: true,
        type: filterTypes.EQUAL,
      },
    ],
  },
];

type DateFilterValue = string | { time: string; value: number };
type FilterPredicateType = keyof typeof filterTypePredicates;

export const filterPredicates = {
  added: function (
    item: GameItem,
    filterValue: DateFilterValue,
    type: string
  ): boolean {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  collection: function (
    item: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];
    const { collection } = item;

    return predicate(
      collection && collection.title ? collection.title : '',
      filterValue
    );
  },

  originalLanguage: function (
    item: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];
    const { originalLanguage } = item;

    return predicate(
      originalLanguage ? originalLanguage.name : '',
      filterValue
    );
  },

  releaseGroups: function (
    item: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];
    const { statistics = {} } = item;
    const { releaseGroups = [] } = statistics;

    return predicate(releaseGroups, filterValue);
  },

  sizeOnDisk: function (
    item: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];
    const { statistics = {} } = item;
    const sizeOnDisk =
      statistics && statistics.sizeOnDisk ? statistics.sizeOnDisk : 0;

    return predicate(sizeOnDisk, filterValue);
  },

  inCinemas: function (
    item: GameItem,
    filterValue: DateFilterValue,
    type: string
  ): boolean {
    return dateFilterPredicate(item.inCinemas, filterValue, type);
  },

  physicalRelease: function (
    item: GameItem,
    filterValue: DateFilterValue,
    type: string
  ): boolean {
    return dateFilterPredicate(item.physicalRelease, filterValue, type);
  },

  digitalRelease: function (
    item: GameItem,
    filterValue: DateFilterValue,
    type: string
  ): boolean {
    return dateFilterPredicate(item.digitalRelease, filterValue, type);
  },

  releaseDate: function (
    item: GameItem,
    filterValue: DateFilterValue,
    type: string
  ): boolean {
    return dateFilterPredicate(item.releaseDate, filterValue, type);
  },

  igdbRating: function (
    { ratings = {} }: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];

    const rating = ratings.igdb ? ratings.igdb.value : 0;

    return predicate(rating * 10, filterValue);
  },

  igdbVotes: function (
    { ratings = {} }: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];

    const rating = ratings.igdb ? ratings.igdb.votes : 0;

    return predicate(rating, filterValue);
  },

  metacriticRating: function (
    { ratings = {} }: GameItem,
    filterValue: unknown,
    type: string
  ): boolean {
    const predicate = filterTypePredicates[type as FilterPredicateType];

    const rating = ratings.metacritic ? ratings.metacritic.value : 0;

    return predicate(rating, filterValue);
  },

  qualityCutoffNotMet: function (item: GameItem): boolean | undefined {
    const { gameFile = {} as GameItem['gameFile'] } = item;

    return gameFile?.qualityCutoffNotMet;
  },
};

export const sortPredicates = {
  status: function (item: GameItem): number {
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

  gameStatus: function (item: GameItem): string {
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
      if (item.gameFile!.qualityCutoffNotMet) {
        result += 4;
      } else {
        result += 8;
      }
      qualityName = item.gameFile!.quality.quality.name;
    }

    return padNumber(result.toString(), 2) + qualityName;
  },

  year: function (item: GameItem): number | undefined {
    return item.year || undefined;
  },

  inCinemas: function (item: GameItem, direction: string): number {
    const { inCinemas } = item;

    if (inCinemas) {
      return moment(inCinemas).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  physicalRelease: function (item: GameItem, direction: string): number {
    const { physicalRelease } = item;

    if (physicalRelease) {
      return moment(physicalRelease).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  digitalRelease: function (item: GameItem, direction: string): number {
    const { digitalRelease } = item;

    if (digitalRelease) {
      return moment(digitalRelease).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  releaseDate: function (item: GameItem, direction: string): number {
    const { releaseDate } = item;

    if (releaseDate) {
      return moment(releaseDate).unix();
    }

    if (direction === sortDirections.DESCENDING) {
      return -1 * Number.MAX_VALUE;
    }

    return Number.MAX_VALUE;
  },

  sizeOnDisk: function (item: GameItem): number {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  },
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null as unknown,
  isSaving: false,
  saveError: null as unknown,
  isDeleting: false,
  deleteError: null as unknown,
  items: [] as GameItem[],
  sortKey: 'sortTitle',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {},
  deleteOptions: {
    addImportExclusion: false,
  },
};

export const persistState = ['games.deleteOptions'];

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
export const saveGame = createThunk(
  SAVE_GAME,
  <T extends SaveGamePayload, TResult>(payload: T): TResult => {
    const newPayload: SaveGamePayload = {
      ...payload,
    };

    if (payload.moveFiles) {
      (newPayload as Record<string, unknown>).queryParams = {
        moveFiles: true,
      };
    }

    delete newPayload.moveFiles;

    return newPayload as unknown as TResult;
  }
);

export const deleteGame = createThunk(
  DELETE_GAME,
  <T extends DeleteGamePayload, TResult>(payload: T): TResult => {
    return {
      ...payload,
      queryParams: {
        deleteFiles: payload.deleteFiles,
        addImportExclusion: payload.addImportExclusion,
      },
    } as unknown as TResult;
  }
);

export const toggleGameMonitored = createThunk(TOGGLE_GAME_MONITORED);
export const saveGameEditor = createThunk(SAVE_GAME_EDITOR);
export const bulkDeleteGame = createThunk(BULK_DELETE_GAME);

export const setGameValue = createAction(
  SET_GAME_VALUE,
  (payload: Record<string, unknown>) => {
    return {
      section,
      ...payload,
    };
  }
);

export const setDeleteOption = createAction(SET_DELETE_OPTION);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_GAMES]: createFetchHandler(section, '/game'),
  [SAVE_GAME]: createSaveProviderHandler(section, '/game'),
  [DELETE_GAME]: (
    getState: () => AppState,
    payload: DeleteGamePayload,
    dispatch: Dispatch
  ) => {
    createRemoveItemHandler(section, '/game')(getState, payload, dispatch);

    if (!payload.collectionIgdbId) {
      return;
    }

    const collectionToUpdate = getState().gameCollections.items.find(
      (collection) => collection.igdbId === payload.collectionIgdbId
    );

    if (!collectionToUpdate) {
      return;
    }

    // Skip updating if the last game in the collection is being deleted
    if (
      collectionToUpdate.games.length - collectionToUpdate.missingGames ===
      1
    ) {
      return;
    }

    const collectionData = {
      ...collectionToUpdate,
      missingGames: collectionToUpdate.missingGames + 1,
    };

    dispatch(
      updateItem({
        section: 'gameCollections',
        ...collectionData,
      })
    );
  },

  [TOGGLE_GAME_MONITORED]: (
    getState: () => AppState,
    payload: ToggleMonitoredPayload,
    dispatch: Dispatch
  ) => {
    const { gameId: id, monitored } = payload;

    const game = _.find(getState().games.items, { id });

    dispatch(
      updateItem({
        id,
        section,
        isSaving: true,
      })
    );

    const promise = createAjaxRequest({
      url: `/game/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...game,
        monitored,
      }),
      dataType: 'json',
    }).request;

    promise.done(() => {
      dispatch(
        updateItem({
          id,
          section,
          isSaving: false,
          monitored,
        })
      );
    });

    promise.fail(() => {
      dispatch(
        updateItem({
          id,
          section,
          isSaving: false,
        })
      );
    });
  },

  [SAVE_GAME_EDITOR]: function (
    _getState: () => AppState,
    payload: unknown,
    dispatch: Dispatch
  ) {
    dispatch(
      set({
        section,
        isSaving: true,
      })
    );

    const promise = createAjaxRequest({
      url: '/game/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json',
    }).request;

    promise.done((data: GameItem[]) => {
      dispatch(
        batchActions([
          ...data.map((game) => {
            return updateItem({
              section: 'games',
              ...game,
            });
          }),

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

  [BULK_DELETE_GAME]: function (
    _getState: () => AppState,
    payload: unknown,
    dispatch: Dispatch
  ) {
    dispatch(
      set({
        section,
        isDeleting: true,
      })
    );

    const promise = createAjaxRequest({
      url: '/game/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json',
    }).request;

    promise.done(() => {
      // SignaR will take care of removing the game from the collection

      dispatch(
        set({
          section,
          isDeleting: false,
          deleteError: null,
        })
      );
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        set({
          section,
          isDeleting: false,
          deleteError: xhr,
        })
      );
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [SET_GAME_VALUE]: createSetSettingValueReducer(section),
    [SET_DELETE_OPTION]: (
      state: typeof defaultState,
      { payload }: { payload: Record<string, unknown> }
    ) => {
      return {
        ...state,
        deleteOptions: {
          ...payload,
        },
      };
    },
  },
  defaultState,
  section
);
