import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import createBulkEditItemHandler from 'Store/Actions/Creators/createBulkEditItemHandler';
import createBulkRemoveItemHandler from 'Store/Actions/Creators/createBulkRemoveItemHandler';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler, {
  createCancelSaveProviderHandler,
} from 'Store/Actions/Creators/createSaveProviderHandler';
import createTestAllProvidersHandler from 'Store/Actions/Creators/createTestAllProvidersHandler';
import createTestProviderHandler, {
  createCancelTestProviderHandler,
} from 'Store/Actions/Creators/createTestProviderHandler';
import createSetClientSideCollectionSortReducer from 'Store/Actions/Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetProviderFieldValueReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValueReducer';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import getSectionState from 'Utilities/State/getSectionState';
import selectProviderSchema from 'Utilities/State/selectProviderSchema';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';

//
// Variables

const section = 'settings.indexers';

//
// Actions Types

export const FETCH_INDEXERS = 'settings/indexers/fetchIndexers';
export const FETCH_INDEXER_SCHEMA = 'settings/indexers/fetchIndexerSchema';
export const SELECT_INDEXER_SCHEMA = 'settings/indexers/selectIndexerSchema';
export const CLONE_INDEXER = 'settings/indexers/cloneIndexer';
export const SET_INDEXER_VALUE = 'settings/indexers/setIndexerValue';
export const SET_INDEXER_FIELD_VALUE = 'settings/indexers/setIndexerFieldValue';
export const SAVE_INDEXER = 'settings/indexers/saveIndexer';
export const CANCEL_SAVE_INDEXER = 'settings/indexers/cancelSaveIndexer';
export const DELETE_INDEXER = 'settings/indexers/deleteIndexer';
export const TEST_INDEXER = 'settings/indexers/testIndexer';
export const CANCEL_TEST_INDEXER = 'settings/indexers/cancelTestIndexer';
export const TEST_ALL_INDEXERS = 'settings/indexers/testAllIndexers';
export const BULK_EDIT_INDEXERS = 'settings/indexers/bulkEditIndexers';
export const BULK_DELETE_INDEXERS = 'settings/indexers/bulkDeleteIndexers';
export const SET_MANAGE_INDEXERS_SORT =
  'settings/indexers/setManageIndexersSort';

//
// Action Creators

export const fetchIndexers = createThunk(FETCH_INDEXERS);
export const fetchIndexerSchema = createThunk(FETCH_INDEXER_SCHEMA);
export const selectIndexerSchema = createAction(SELECT_INDEXER_SCHEMA);
export const cloneIndexer = createAction(CLONE_INDEXER);

export const saveIndexer = createThunk(SAVE_INDEXER);
export const cancelSaveIndexer = createThunk(CANCEL_SAVE_INDEXER);
export const deleteIndexer = createThunk(DELETE_INDEXER);
export const testIndexer = createThunk(TEST_INDEXER);
export const cancelTestIndexer = createThunk(CANCEL_TEST_INDEXER);
export const testAllIndexers = createThunk(TEST_ALL_INDEXERS);
export const bulkEditIndexers = createThunk(BULK_EDIT_INDEXERS);
export const bulkDeleteIndexers = createThunk(BULK_DELETE_INDEXERS);
export const setManageIndexersSort = createAction(SET_MANAGE_INDEXERS_SORT);

export const setIndexerValue = createAction(
  SET_INDEXER_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

export const setIndexerFieldValue = createAction(
  SET_INDEXER_FIELD_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

//
// Details

interface IndexerField {
  privacy?: string;
  value?: unknown;
}

interface IndexerItem {
  id: number;
  name: string;
  fields: IndexerField[];
  implementationName?: string;
  supportsRss?: boolean;
  supportsSearch?: boolean;
}

interface IndexerSchema {
  name?: string;
  implementationName?: string;
  enableRss?: boolean;
  enableAutomaticSearch?: boolean;
  enableInteractiveSearch?: boolean;
  supportsRss?: boolean;
  supportsSearch?: boolean;
  presetName?: string;
  fields?: IndexerField[];
}

interface SelectIndexerSchemaPayload {
  implementation: string;
  presetName?: string;
  implementationName: string;
}

interface CloneIndexerPayload {
  id: number;
}

type State = Record<string, unknown>;

interface IndexerSectionState extends State {
  items: IndexerItem[];
  selectedSchema: IndexerSchema;
  pendingChanges: Record<string, unknown>;
}

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    isSchemaFetching: false,
    isSchemaPopulated: false,
    schemaError: null as unknown,
    schema: [] as IndexerSchema[],
    selectedSchema: {} as IndexerSchema,
    isSaving: false,
    saveError: null as unknown,
    isDeleting: false,
    deleteError: null as unknown,
    isTesting: false,
    isTestingAll: false,
    items: [] as IndexerItem[],
    pendingChanges: {} as Record<string, unknown>,
    sortKey: 'name',
    sortDirection: sortDirections.ASCENDING,
    sortPredicates: {
      name: ({ name }: { name: string }) => {
        return name.toLocaleLowerCase();
      },
    },
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_INDEXERS]: createFetchHandler(section, '/indexer'),
    [FETCH_INDEXER_SCHEMA]: createFetchSchemaHandler(
      section,
      '/indexer/schema'
    ),

    [SAVE_INDEXER]: createSaveProviderHandler(section, '/indexer'),
    [CANCEL_SAVE_INDEXER]: createCancelSaveProviderHandler(section),
    [DELETE_INDEXER]: createRemoveItemHandler(section, '/indexer'),
    [TEST_INDEXER]: createTestProviderHandler(section, '/indexer'),
    [CANCEL_TEST_INDEXER]: createCancelTestProviderHandler(section),
    [TEST_ALL_INDEXERS]: createTestAllProvidersHandler(section, '/indexer'),

    [BULK_DELETE_INDEXERS]: createBulkRemoveItemHandler(
      section,
      '/indexer/bulk'
    ),
    [BULK_EDIT_INDEXERS]: createBulkEditItemHandler(section, '/indexer/bulk'),
  },

  //
  // Reducers

  reducers: {
    [SET_INDEXER_VALUE]: createSetSettingValueReducer(section),
    [SET_INDEXER_FIELD_VALUE]: createSetProviderFieldValueReducer(section),

    [SELECT_INDEXER_SCHEMA]: (
      state: State,
      { payload }: { payload: SelectIndexerSchemaPayload }
    ) => {
      return selectProviderSchema(state, section, payload, (selectedSchema) => {
        selectedSchema.name = payload.presetName ?? payload.implementationName;
        (selectedSchema as IndexerSchema).implementationName =
          payload.implementationName;
        (selectedSchema as IndexerSchema).enableRss = (
          selectedSchema as IndexerSchema
        ).supportsRss;
        (selectedSchema as IndexerSchema).enableAutomaticSearch = (
          selectedSchema as IndexerSchema
        ).supportsSearch;
        (selectedSchema as IndexerSchema).enableInteractiveSearch = (
          selectedSchema as IndexerSchema
        ).supportsSearch;

        return selectedSchema;
      });
    },

    [CLONE_INDEXER]: function (
      state: State,
      { payload }: { payload: CloneIndexerPayload }
    ) {
      const id = payload.id;
      const newState = getSectionState(state, section) as IndexerSectionState;
      const item = newState.items.find((i) => i.id === id);

      if (!item) {
        return state;
      }

      // Use selectedSchema so `createProviderSettingsSelector` works properly
      const selectedSchema: IndexerSchema = { ...item };
      delete (selectedSchema as { id?: number }).id;
      delete selectedSchema.name;

      selectedSchema.fields = (selectedSchema.fields ?? []).map((field) => {
        const newField = { ...field };

        if (newField.privacy === 'apiKey' || newField.privacy === 'password') {
          newField.value = '';
        }

        return newField;
      });

      newState.selectedSchema = selectedSchema;

      // Set the name in pendingChanges
      newState.pendingChanges = {
        name: translate('DefaultNameCopiedProfile', { name: item.name }),
      };

      return updateSectionState(state, section, newState);
    },

    [SET_MANAGE_INDEXERS_SORT]:
      createSetClientSideCollectionSortReducer(section),
  },
};
