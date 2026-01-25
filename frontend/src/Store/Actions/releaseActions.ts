import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import AppState from 'App/State/AppState';
import {
  filterBuilderTypes,
  filterBuilderValueTypes,
  filterTypePredicates,
  filterTypes,
  sortDirections,
} from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import sortByProp from 'Utilities/Array/sortByProp';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

export const section = 'releases';

let abortCurrentRequest: (() => void) | null = null;

interface Language {
  id: number;
  name: string;
}

interface Quality {
  quality: {
    id: number;
  };
}

interface ReleaseItem {
  guid: string;
  ageMinutes: number;
  seeders?: number;
  leechers?: number;
  languages: Language[];
  indexerFlags: unknown[];
  rejections: unknown[];
  releaseWeight: number;
  quality: Quality;
  [key: string]: unknown;
}

interface SortPredicate {
  (item: ReleaseItem, direction: string): number;
}

interface FilterPredicate {
  (item: ReleaseItem, value: unknown, type: string): boolean;
}

interface Filter {
  key: string;
  label: () => string;
  filters: unknown[];
}

interface FilterBuilderProp {
  name: string;
  label: () => string;
  type: string;
  valueType?: string;
  optionsSelector?: (items: ReleaseItem[]) => { id: string; name: string }[];
}

export interface ReleasesState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: ReleaseItem[];
  sortKey: string;
  sortDirection: string;
  sortPredicates: Record<string, SortPredicate>;
  filters: Filter[];
  filterPredicates: Record<string, FilterPredicate>;
  filterBuilderProps: FilterBuilderProp[];
  selectedFilterKey: string;
}

interface FetchPayload {
  id?: number;
  [key: string]: unknown;
}

interface GrabPayload {
  guid: string;
  [key: string]: unknown;
}

interface UpdateReleasePayload {
  guid: string;
  isGrabbing?: boolean;
  isGrabbed?: boolean;
  grabError?: string | null;
}

export const defaultState: ReleasesState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],
  sortKey: 'releaseWeight',
  sortDirection: sortDirections.ASCENDING,
  sortPredicates: {
    age: function (item: ReleaseItem) {
      return item.ageMinutes;
    },

    peers: function (item: ReleaseItem) {
      const seeders = item.seeders || 0;
      const leechers = item.leechers || 0;

      return seeders * 1000000 + leechers;
    },

    languages: function (item: ReleaseItem) {
      if (item.languages.length > 1) {
        return 10000;
      }

      return item.languages[0]?.id ?? 0;
    },

    indexerFlags: function (item: ReleaseItem) {
      const indexerFlags = item.indexerFlags;
      const releaseWeight = item.releaseWeight;

      if (indexerFlags.length === 0) {
        return releaseWeight + 1000000;
      }

      return releaseWeight;
    },

    rejections: function (item: ReleaseItem) {
      const rejections = item.rejections;
      const releaseWeight = item.releaseWeight;

      if (rejections.length !== 0) {
        return releaseWeight + 1000000;
      }

      return releaseWeight;
    },
  },

  filters: [
    {
      key: 'all',
      label: () => translate('All'),
      filters: [],
    },
  ],

  filterPredicates: {
    quality: function (item: ReleaseItem, value: unknown, type: string) {
      const qualityId = item.quality.quality.id;

      if (type === filterTypes.EQUAL) {
        return qualityId === value;
      }

      if (type === filterTypes.NOT_EQUAL) {
        return qualityId !== value;
      }

      return false;
    },

    languages: function (
      item: ReleaseItem,
      filterValue: unknown,
      type: string
    ) {
      const predicate =
        filterTypePredicates[type as keyof typeof filterTypePredicates];

      const languages = item.languages.map((language) => language.name);

      return predicate(languages, filterValue);
    },

    peers: function (item: ReleaseItem, value: unknown, type: string) {
      const predicate =
        filterTypePredicates[type as keyof typeof filterTypePredicates];
      const seeders = item.seeders || 0;
      const leechers = item.leechers || 0;

      return predicate(seeders + leechers, value);
    },

    rejectionCount: function (item: ReleaseItem, value: unknown, type: string) {
      const rejectionCount = item.rejections.length;
      const numValue = value as number;

      switch (type) {
        case filterTypes.EQUAL:
          return rejectionCount === numValue;

        case filterTypes.GREATER_THAN:
          return rejectionCount > numValue;

        case filterTypes.GREATER_THAN_OR_EQUAL:
          return rejectionCount >= numValue;

        case filterTypes.LESS_THAN:
          return rejectionCount < numValue;

        case filterTypes.LESS_THAN_OR_EQUAL:
          return rejectionCount <= numValue;

        case filterTypes.NOT_EQUAL:
          return rejectionCount !== numValue;

        default:
          return false;
      }
    },
  },

  filterBuilderProps: [
    {
      name: 'title',
      label: () => translate('Title'),
      type: filterBuilderTypes.STRING,
    },
    {
      name: 'age',
      label: () => translate('Age'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'protocol',
      label: () => translate('Protocol'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.PROTOCOL,
    },
    {
      name: 'indexerId',
      label: () => translate('Indexer'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.INDEXER,
    },
    {
      name: 'size',
      label: () => translate('Size'),
      type: filterBuilderTypes.NUMBER,
      valueType: filterBuilderValueTypes.BYTES,
    },
    {
      name: 'seeders',
      label: () => translate('Seeders'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'peers',
      label: () => translate('Peers'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'quality',
      label: () => translate('Quality'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY,
    },
    {
      name: 'languages',
      label: () => translate('Languages'),
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function (items: ReleaseItem[]) {
        const genreList = items.reduce(
          (acc: { id: string; name: string }[], release) => {
            release.languages.forEach((language) => {
              acc.push({
                id: language.name,
                name: language.name,
              });
            });

            return acc;
          },
          []
        );

        return genreList.sort(sortByProp('name'));
      },
    },
    {
      name: 'customFormatScore',
      label: () => translate('CustomFormatScore'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'rejectionCount',
      label: () => translate('RejectionCount'),
      type: filterBuilderTypes.NUMBER,
    },
    {
      name: 'gameRequested',
      label: () => translate('GameRequested'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL,
    },
  ],
  selectedFilterKey: 'all',
};

export const persistState = [
  'releases.customFilters',
  'releases.selectedFilterKey',
];

export const FETCH_RELEASES = 'releases/fetchReleases';
export const CANCEL_FETCH_RELEASES = 'releases/cancelFetchReleases';
export const SET_RELEASES_SORT = 'releases/setReleasesSort';
export const CLEAR_RELEASES = 'releases/clearReleases';
export const GRAB_RELEASE = 'releases/grabRelease';
export const UPDATE_RELEASE = 'releases/updateRelease';
export const SET_RELEASES_FILTER = 'releases/setGameReleasesFilter';

export const fetchReleases = createThunk(FETCH_RELEASES);
export const cancelFetchReleases = createThunk(CANCEL_FETCH_RELEASES);
export const setReleasesSort = createAction(SET_RELEASES_SORT);
export const clearReleases = createAction(CLEAR_RELEASES);
export const grabRelease = createThunk(GRAB_RELEASE);
export const updateRelease = createAction(UPDATE_RELEASE);
export const setReleasesFilter = createAction(SET_RELEASES_FILTER);

const fetchReleasesHelper = createFetchHandler(section, '/release');

export const actionHandlers = handleThunks({
  [FETCH_RELEASES]: function (
    getState: () => AppState,
    payload: FetchPayload,
    dispatch: Dispatch
  ) {
    const abortRequest = fetchReleasesHelper(getState, payload, dispatch);

    abortCurrentRequest = abortRequest;
  },

  [CANCEL_FETCH_RELEASES]: function () {
    if (abortCurrentRequest) {
      abortCurrentRequest();
      abortCurrentRequest = null;
    }
  },

  [GRAB_RELEASE]: function (
    _getState: () => AppState,
    payload: GrabPayload,
    dispatch: Dispatch
  ) {
    const guid = payload.guid;

    dispatch(updateRelease({ guid, isGrabbing: true }));

    const promise = createAjaxRequest({
      url: '/release',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(payload),
    }).request;

    promise.done(() => {
      dispatch(
        updateRelease({
          guid,
          isGrabbing: false,
          isGrabbed: true,
          grabError: null,
        })
      );
    });

    promise.fail((xhr: unknown) => {
      const xhrWithResponse = xhr as {
        responseJSON?: { message?: string };
      };
      const grabError =
        (xhrWithResponse.responseJSON && xhrWithResponse.responseJSON.message) ||
        'Failed to add to download queue';

      dispatch(
        updateRelease({
          guid,
          isGrabbing: false,
          isGrabbed: false,
          grabError,
        })
      );
    });
  },
});

export const reducers = createHandleActions(
  {
    [CLEAR_RELEASES]: (state: ReleasesState) => {
      const { selectedFilterKey, ...otherDefaultState } = defaultState;

      return Object.assign({}, state, otherDefaultState);
    },

    [UPDATE_RELEASE]: (
      state: ReleasesState,
      { payload }: { payload: UpdateReleasePayload }
    ) => {
      const guid = payload.guid;
      const newState = Object.assign({}, state);
      const items = newState.items;
      const index = items.findIndex((item) => item.guid === guid);

      if (index >= 0) {
        const item = Object.assign({}, items[index], payload);

        newState.items = [...items];
        newState.items.splice(index, 1, item);
      }

      return newState;
    },

    [SET_RELEASES_FILTER]: createSetClientSideCollectionFilterReducer(section),
    [SET_RELEASES_SORT]: createSetClientSideCollectionSortReducer(section),
  },
  defaultState,
  section
);
