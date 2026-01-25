import _ from 'lodash';
import moment from 'moment';
import React from 'react';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import Icon from 'Components/Icon';
import {
  filterBuilderTypes,
  filterBuilderValueTypes,
  icons,
  sortDirections,
} from 'Helpers/Props';
import { AppDispatch, createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import serverSideCollectionHandlers from 'Utilities/serverSideCollectionHandlers';
import translate from 'Utilities/String/translate';
import { set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createServerSideCollectionHandlers from './Creators/createServerSideCollectionHandlers';
import createClearReducer from './Creators/Reducers/createClearReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

export const section = 'queue';
const status = `${section}.status`;
const details = `${section}.details`;
const paged = `${section}.paged`;

interface QueueItem {
  timeLeft?: string;
  [key: string]: unknown;
}

interface Column {
  name: string;
  label?: (() => string) | React.ReactElement<any>;
  columnLabel?: () => string;
  isSortable?: boolean;
  isVisible: boolean;
  isModifiable?: boolean;
}

interface Filter {
  key: string;
  label: string;
  filters: unknown[];
}

interface FilterBuilderProp {
  name: string;
  label: () => string;
  type: string;
  valueType?: string;
}

interface QueueOptions {
  includeUnknownGameItems: boolean;
}

interface QueueStatusState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  item: Record<string, unknown>;
}

interface QueueDetailsState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: unknown[];
  params: Record<string, unknown>;
}

interface QueuePagedState {
  isFetching: boolean;
  isPopulated: boolean;
  pageSize: number;
  sortKey: string;
  sortDirection: string;
  error: unknown;
  items: unknown[];
  isGrabbing: boolean;
  isRemoving: boolean;
  columns: Column[];
  selectedFilterKey: string;
  filters: Filter[];
  filterBuilderProps: FilterBuilderProp[];
}

export interface QueueState {
  options: QueueOptions;
  status: QueueStatusState;
  details: QueueDetailsState;
  paged: QueuePagedState;
  sortPredicates: Record<
    string,
    (item: QueueItem, direction: string) => number
  >;
}

interface GrabPayload {
  id: number;
}

interface GrabItemsPayload {
  ids: number[];
}

interface RemovePayload {
  id: number;
  remove: boolean;
  blocklist: boolean;
  skipRedownload: boolean;
  changeCategory: boolean;
}

interface RemoveItemsPayload {
  ids: number[];
  remove: boolean;
  blocklist: boolean;
  skipRedownload: boolean;
  changeCategory: boolean;
}

interface SetQueueOptionPayload {
  [key: string]: unknown;
}

export const defaultState: QueueState = {
  options: {
    includeUnknownGameItems: true,
  },

  status: {
    isFetching: false,
    isPopulated: false,
    error: null,
    item: {},
  },

  details: {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: [],
    params: {},
  },

  paged: {
    isFetching: false,
    isPopulated: false,
    pageSize: 20,
    sortKey: 'timeLeft',
    sortDirection: sortDirections.ASCENDING,
    error: null,
    items: [],
    isGrabbing: false,
    isRemoving: false,

    columns: [
      {
        name: 'status',
        columnLabel: () => translate('Status'),
        isSortable: true,
        isVisible: true,
        isModifiable: false,
      },
      {
        name: 'games.sortTitle',
        label: () => translate('Game'),
        isSortable: true,
        isVisible: true,
      },
      {
        name: 'year',
        label: () => translate('Year'),
        isSortable: true,
        isVisible: true,
      },
      {
        name: 'languages',
        label: () => translate('Languages'),
        isSortable: true,
        isVisible: true,
      },
      {
        name: 'quality',
        label: () => translate('Quality'),
        isSortable: true,
        isVisible: true,
      },
      {
        name: 'version',
        label: () => translate('Version'),
        isSortable: false,
        isVisible: true,
      },
      {
        name: 'customFormats',
        label: () => translate('Formats'),
        isSortable: false,
        isVisible: true,
      },
      {
        name: 'customFormatScore',
        columnLabel: () => translate('CustomFormatScore'),
        label: React.createElement(Icon, {
          name: icons.SCORE,
          title: () => translate('CustomFormatScore'),
        }),
        isVisible: false,
      },
      {
        name: 'protocol',
        label: () => translate('Protocol'),
        isSortable: true,
        isVisible: false,
      },
      {
        name: 'indexer',
        label: () => translate('Indexer'),
        isSortable: true,
        isVisible: false,
      },
      {
        name: 'downloadClient',
        label: () => translate('DownloadClient'),
        isSortable: true,
        isVisible: false,
      },
      {
        name: 'size',
        label: () => translate('Size'),
        isSortable: true,
        isVisible: false,
      },
      {
        name: 'title',
        label: () => translate('ReleaseTitle'),
        isSortable: true,
        isVisible: false,
      },
      {
        name: 'outputPath',
        label: () => translate('OutputPath'),
        isSortable: false,
        isVisible: false,
      },
      {
        name: 'estimatedCompletionTime',
        label: () => translate('Timeleft'),
        isSortable: true,
        isVisible: true,
      },
      {
        name: 'added',
        label: () => translate('Added'),
        isSortable: true,
        isVisible: false,
      },
      {
        name: 'progress',
        label: () => translate('Progress'),
        isSortable: true,
        isVisible: true,
      },
      {
        name: 'actions',
        columnLabel: () => translate('Actions'),
        isVisible: true,
        isModifiable: false,
      },
    ],

    selectedFilterKey: 'all',

    filters: [
      {
        key: 'all',
        label: 'All',
        filters: [],
      },
    ],

    filterBuilderProps: [
      {
        name: 'gameIds',
        label: () => translate('Game'),
        type: filterBuilderTypes.EQUAL,
        valueType: filterBuilderValueTypes.GAME,
      },
      {
        name: 'quality',
        label: () => translate('Quality'),
        type: filterBuilderTypes.EQUAL,
        valueType: filterBuilderValueTypes.QUALITY,
      },
      {
        name: 'languages',
        label: () => translate('Languages'),
        type: filterBuilderTypes.CONTAINS,
        valueType: filterBuilderValueTypes.LANGUAGE,
      },
      {
        name: 'protocol',
        label: () => translate('Protocol'),
        type: filterBuilderTypes.EQUAL,
        valueType: filterBuilderValueTypes.PROTOCOL,
      },
      {
        name: 'status',
        label: () => translate('Status'),
        type: filterBuilderTypes.EQUAL,
        valueType: filterBuilderValueTypes.QUEUE_STATUS,
      },
    ],
  },
  sortPredicates: {
    estimatedCompletionTime: function (item: QueueItem) {
      return moment.duration(item.timeLeft).asMilliseconds();
    },
  },
};

export const persistState = [
  'queue.options',
  'queue.paged.pageSize',
  'queue.paged.sortKey',
  'queue.paged.sortDirection',
  'queue.paged.columns',
  'queue.paged.selectedFilterKey',
];

function fetchDataAugmenter(
  getState: () => unknown,
  _payload: unknown,
  data: Record<string, unknown>
) {
  data.includeUnknownGameItems = (
    getState() as AppState
  ).queue.options.includeUnknownGameItems;
}

export const FETCH_QUEUE_STATUS = 'queue/fetchQueueStatus';

export const FETCH_QUEUE_DETAILS = 'queue/fetchQueueDetails';
export const CLEAR_QUEUE_DETAILS = 'queue/clearQueueDetails';

export const FETCH_QUEUE = 'queue/fetchQueue';
export const GOTO_FIRST_QUEUE_PAGE = 'queue/gotoQueueFirstPage';
export const GOTO_PREVIOUS_QUEUE_PAGE = 'queue/gotoQueuePreviousPage';
export const GOTO_NEXT_QUEUE_PAGE = 'queue/gotoQueueNextPage';
export const GOTO_LAST_QUEUE_PAGE = 'queue/gotoQueueLastPage';
export const GOTO_QUEUE_PAGE = 'queue/gotoQueuePage';
export const SET_QUEUE_SORT = 'queue/setQueueSort';
export const SET_QUEUE_FILTER = 'queue/setQueueFilter';
export const SET_QUEUE_TABLE_OPTION = 'queue/setQueueTableOption';
export const SET_QUEUE_OPTION = 'queue/setQueueOption';
export const CLEAR_QUEUE = 'queue/clearQueue';

export const GRAB_QUEUE_ITEM = 'queue/grabQueueItem';
export const GRAB_QUEUE_ITEMS = 'queue/grabQueueItems';
export const REMOVE_QUEUE_ITEM = 'queue/removeQueueItem';
export const REMOVE_QUEUE_ITEMS = 'queue/removeQueueItems';

export const fetchQueueStatus = createThunk(FETCH_QUEUE_STATUS);

export const fetchQueueDetails = createThunk(FETCH_QUEUE_DETAILS);
export const clearQueueDetails = createAction(CLEAR_QUEUE_DETAILS);

export const fetchQueue = createThunk(FETCH_QUEUE);
export const gotoQueueFirstPage = createThunk(GOTO_FIRST_QUEUE_PAGE);
export const gotoQueuePreviousPage = createThunk(GOTO_PREVIOUS_QUEUE_PAGE);
export const gotoQueueNextPage = createThunk(GOTO_NEXT_QUEUE_PAGE);
export const gotoQueueLastPage = createThunk(GOTO_LAST_QUEUE_PAGE);
export const gotoQueuePage = createThunk(GOTO_QUEUE_PAGE);
export const setQueueSort = createThunk(SET_QUEUE_SORT);
export const setQueueFilter = createThunk(SET_QUEUE_FILTER);
export const setQueueTableOption = createAction(SET_QUEUE_TABLE_OPTION);
export const setQueueOption = createAction(SET_QUEUE_OPTION);
export const clearQueue = createAction(CLEAR_QUEUE);

export const grabQueueItem = createThunk(GRAB_QUEUE_ITEM);
export const grabQueueItems = createThunk(GRAB_QUEUE_ITEMS);
export const removeQueueItem = createThunk(REMOVE_QUEUE_ITEM);
export const removeQueueItems = createThunk(REMOVE_QUEUE_ITEMS);

const fetchQueueDetailsHelper = createFetchHandler(details, '/queue/details');

export const actionHandlers = handleThunks({
  [FETCH_QUEUE_STATUS]: createFetchHandler(status, '/queue/status'),

  [FETCH_QUEUE_DETAILS]: function (
    getState: () => AppState,
    payload: Record<string, unknown>,
    dispatch: AppDispatch
  ) {
    let params: Record<string, unknown> = payload;

    if (params && !_.isEmpty(params)) {
      dispatch(set({ section: details, params }));
    } else {
      params = getState().queue.details.params as Record<string, unknown>;
    }

    fetchQueueDetailsHelper(getState, params, dispatch);
  },

  ...createServerSideCollectionHandlers(
    paged,
    '/queue',
    fetchQueue,
    {
      [serverSideCollectionHandlers.FETCH]: FETCH_QUEUE,
      [serverSideCollectionHandlers.FIRST_PAGE]: GOTO_FIRST_QUEUE_PAGE,
      [serverSideCollectionHandlers.PREVIOUS_PAGE]: GOTO_PREVIOUS_QUEUE_PAGE,
      [serverSideCollectionHandlers.NEXT_PAGE]: GOTO_NEXT_QUEUE_PAGE,
      [serverSideCollectionHandlers.LAST_PAGE]: GOTO_LAST_QUEUE_PAGE,
      [serverSideCollectionHandlers.EXACT_PAGE]: GOTO_QUEUE_PAGE,
      [serverSideCollectionHandlers.SORT]: SET_QUEUE_SORT,
      [serverSideCollectionHandlers.FILTER]: SET_QUEUE_FILTER,
    },
    fetchDataAugmenter
  ),

  [GRAB_QUEUE_ITEM]: function (
    _getState: () => AppState,
    payload: GrabPayload,
    dispatch: AppDispatch
  ) {
    const id = payload.id;

    dispatch(updateItem({ section: paged, id, isGrabbing: true }));

    const promise = createAjaxRequest({
      url: `/queue/grab/${id}`,
      method: 'POST',
    }).request;

    promise.done(() => {
      dispatch(fetchQueue());
      dispatch(
        set({
          section: paged,
          isGrabbing: false,
          grabError: null,
        })
      );
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        updateItem({
          section: paged,
          id,
          isGrabbing: false,
          grabError: xhr,
        })
      );
    });
  },

  [GRAB_QUEUE_ITEMS]: function (
    _getState: () => AppState,
    payload: GrabItemsPayload,
    dispatch: AppDispatch
  ) {
    const ids = payload.ids;

    dispatch(
      batchActions([
        ...ids.map((id) => {
          return updateItem({
            section: paged,
            id,
            isGrabbing: true,
          });
        }),

        set({
          section: paged,
          isGrabbing: true,
        }),
      ])
    );

    const promise = createAjaxRequest({
      url: '/queue/grab/bulk',
      method: 'POST',
      dataType: 'json',
      data: JSON.stringify(payload),
    }).request;

    promise.done(() => {
      dispatch(fetchQueue());

      dispatch(
        batchActions([
          ...ids.map((id) => {
            return updateItem({
              section: paged,
              id,
              isGrabbing: false,
              grabError: null,
            });
          }),

          set({
            section: paged,
            isGrabbing: false,
            grabError: null,
          }),
        ])
      );
    });

    promise.fail(() => {
      dispatch(
        batchActions([
          ...ids.map((id) => {
            return updateItem({
              section: paged,
              id,
              isGrabbing: false,
              grabError: null,
            });
          }),

          set({ section: paged, isGrabbing: false }),
        ])
      );
    });
  },

  [REMOVE_QUEUE_ITEM]: function (
    _getState: () => AppState,
    payload: RemovePayload,
    dispatch: AppDispatch
  ) {
    const { id, remove, blocklist, skipRedownload, changeCategory } = payload;

    dispatch(updateItem({ section: paged, id, isRemoving: true }));

    const promise = createAjaxRequest({
      url: `/queue/${id}?removeFromClient=${remove}&blocklist=${blocklist}&skipRedownload=${skipRedownload}&changeCategory=${changeCategory}`,
      method: 'DELETE',
    }).request;

    promise.done(() => {
      dispatch(fetchQueue());
    });

    promise.fail(() => {
      dispatch(updateItem({ section: paged, id, isRemoving: false }));
    });
  },

  [REMOVE_QUEUE_ITEMS]: function (
    _getState: () => AppState,
    payload: RemoveItemsPayload,
    dispatch: AppDispatch
  ) {
    const { ids, remove, blocklist, skipRedownload, changeCategory } = payload;

    dispatch(
      batchActions([
        ...ids.map((id) => {
          return updateItem({
            section: paged,
            id,
            isRemoving: true,
          });
        }),

        set({ section: paged, isRemoving: true }),
      ])
    );

    const promise = createAjaxRequest({
      url: `/queue/bulk?removeFromClient=${remove}&blocklist=${blocklist}&skipRedownload=${skipRedownload}&changeCategory=${changeCategory}`,
      method: 'DELETE',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify({ ids }),
    }).request;

    promise.done(() => {
      dispatch(fetchQueue());

      dispatch(set({ section: paged, isRemoving: false }));
    });

    promise.fail(() => {
      dispatch(
        batchActions([
          ...ids.map((id) => {
            return updateItem({
              section: paged,
              id,
              isRemoving: false,
            });
          }),

          set({ section: paged, isRemoving: false }),
        ])
      );
    });
  },
});

export const reducers = createHandleActions(
  {
    [CLEAR_QUEUE_DETAILS]: createClearReducer(details, defaultState.details),

    [SET_QUEUE_TABLE_OPTION]: createSetTableOptionReducer(paged),

    [SET_QUEUE_OPTION]: function (
      state: QueueState,
      { payload }: { payload: SetQueueOptionPayload }
    ) {
      const queueOptions = state.options;

      return {
        ...state,
        options: {
          ...queueOptions,
          ...payload,
        },
      };
    },

    [CLEAR_QUEUE]: createClearReducer(paged, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: [],
      totalPages: 0,
      totalRecords: 0,
    }),
  },
  defaultState,
  section
);
