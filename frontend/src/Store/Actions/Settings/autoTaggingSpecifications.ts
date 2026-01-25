import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createClearReducer from 'Store/Actions/Creators/Reducers/createClearReducer';
import createSetProviderFieldValueReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValueReducer';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import getNextId from 'Utilities/State/getNextId';
import getProviderState from 'Utilities/State/getProviderState';
import getSectionState from 'Utilities/State/getSectionState';
import selectProviderSchema from 'Utilities/State/selectProviderSchema';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';
import { removeItem, set, update, updateItem } from '../baseActions';

interface SettingValuePayload {
  name: string;
  value: unknown;
}

interface IdPayload {
  id: number;
}

interface SchemaPayload {
  implementation: string;
}

interface SpecificationItem {
  id: number;
  name: string;
  [key: string]: unknown;
}

interface SectionState {
  items: SpecificationItem[];
  itemMap: Record<number, number>;
  [key: string]: unknown;
}

const section = 'settings.autoTaggingSpecifications';

export const FETCH_AUTO_TAGGING_SPECIFICATIONS =
  'settings/autoTaggingSpecifications/fetchAutoTaggingSpecifications';
export const FETCH_AUTO_TAGGING_SPECIFICATION_SCHEMA =
  'settings/autoTaggingSpecifications/fetchAutoTaggingSpecificationSchema';
export const SELECT_AUTO_TAGGING_SPECIFICATION_SCHEMA =
  'settings/autoTaggingSpecifications/selectAutoTaggingSpecificationSchema';
export const SET_AUTO_TAGGING_SPECIFICATION_VALUE =
  'settings/autoTaggingSpecifications/setAutoTaggingSpecificationValue';
export const SET_AUTO_TAGGING_SPECIFICATION_FIELD_VALUE =
  'settings/autoTaggingSpecifications/setAutoTaggingSpecificationFieldValue';
export const SAVE_AUTO_TAGGING_SPECIFICATION =
  'settings/autoTaggingSpecifications/saveAutoTaggingSpecification';
export const DELETE_AUTO_TAGGING_SPECIFICATION =
  'settings/autoTaggingSpecifications/deleteAutoTaggingSpecification';
export const DELETE_ALL_AUTO_TAGGING_SPECIFICATION =
  'settings/autoTaggingSpecifications/deleteAllAutoTaggingSpecification';
export const CLONE_AUTO_TAGGING_SPECIFICATION =
  'settings/autoTaggingSpecifications/cloneAutoTaggingSpecification';
export const CLEAR_AUTO_TAGGING_SPECIFICATIONS =
  'settings/autoTaggingSpecifications/clearAutoTaggingSpecifications';
export const CLEAR_AUTO_TAGGING_SPECIFICATION_PENDING =
  'settings/autoTaggingSpecifications/clearAutoTaggingSpecificationPending';

export const fetchAutoTaggingSpecifications = createThunk(
  FETCH_AUTO_TAGGING_SPECIFICATIONS
);
export const fetchAutoTaggingSpecificationSchema = createThunk(
  FETCH_AUTO_TAGGING_SPECIFICATION_SCHEMA
);
export const selectAutoTaggingSpecificationSchema = createAction(
  SELECT_AUTO_TAGGING_SPECIFICATION_SCHEMA
);

export const saveAutoTaggingSpecification = createThunk(
  SAVE_AUTO_TAGGING_SPECIFICATION
);
export const deleteAutoTaggingSpecification = createThunk(
  DELETE_AUTO_TAGGING_SPECIFICATION
);
export const deleteAllAutoTaggingSpecification = createThunk(
  DELETE_ALL_AUTO_TAGGING_SPECIFICATION
);

export const setAutoTaggingSpecificationValue = createAction(
  SET_AUTO_TAGGING_SPECIFICATION_VALUE,
  (payload: SettingValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

export const setAutoTaggingSpecificationFieldValue = createAction(
  SET_AUTO_TAGGING_SPECIFICATION_FIELD_VALUE,
  (payload: SettingValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

export const cloneAutoTaggingSpecification = createAction(
  CLONE_AUTO_TAGGING_SPECIFICATION
);

export const clearAutoTaggingSpecification = createAction(
  CLEAR_AUTO_TAGGING_SPECIFICATIONS
);

export const clearAutoTaggingSpecificationPending = createThunk(
  CLEAR_AUTO_TAGGING_SPECIFICATION_PENDING
);

export default {
  defaultState: {
    isPopulated: false,
    error: null,
    isSchemaFetching: false,
    isSchemaPopulated: false,
    schemaError: null,
    schema: [],
    selectedSchema: {},
    isSaving: false,
    saveError: null,
    items: [],
    pendingChanges: {},
  },

  actionHandlers: {
    [FETCH_AUTO_TAGGING_SPECIFICATION_SCHEMA]: createFetchSchemaHandler(
      section,
      '/autoTagging/schema'
    ),

    [FETCH_AUTO_TAGGING_SPECIFICATIONS]: (
      getState: () => AppState,
      payload: IdPayload,
      dispatch: Dispatch
    ) => {
      let tags: SpecificationItem[] = [];
      if (payload.id) {
        const cfState = getSectionState(
          getState(),
          'settings.autoTaggings',
          true
        ) as SectionState;
        const cf = cfState.items[cfState.itemMap[payload.id]] as unknown as {
          specifications: Omit<SpecificationItem, 'id'>[];
        };
        tags = cf.specifications.map((tag, i) => {
          return {
            id: i + 1,
            ...tag,
          } as SpecificationItem;
        });
      }

      dispatch(
        batchActions([
          update({ section, data: tags }),
          set({
            section,
            isPopulated: true,
          }),
        ])
      );
    },

    [SAVE_AUTO_TAGGING_SPECIFICATION]: (
      getState: () => AppState,
      payload: IdPayload & Record<string, unknown>,
      dispatch: Dispatch
    ) => {
      const { id, ...otherPayload } = payload;

      const saveData = getProviderState(
        { id, ...otherPayload },
        getState as () => Record<string, unknown>,
        section,
        false
      ) as SpecificationItem;

      if (!saveData.id) {
        saveData.id = getNextId(
          getState().settings.autoTaggingSpecifications.items
        );
      }

      dispatch(
        batchActions([
          updateItem({ section, ...saveData }),
          set({
            section,
            pendingChanges: {},
          }),
        ])
      );
    },

    [DELETE_AUTO_TAGGING_SPECIFICATION]: (
      _getState: () => AppState,
      payload: IdPayload,
      dispatch: Dispatch
    ) => {
      const id = payload.id;
      return dispatch(removeItem({ section, id }));
    },

    [DELETE_ALL_AUTO_TAGGING_SPECIFICATION]: (
      _getState: () => AppState,
      _payload: unknown,
      dispatch: Dispatch
    ) => {
      return dispatch(
        set({
          section,
          items: [],
        })
      );
    },

    [CLEAR_AUTO_TAGGING_SPECIFICATION_PENDING]: (
      _getState: () => AppState,
      _payload: unknown,
      dispatch: Dispatch
    ) => {
      return dispatch(
        set({
          section,
          pendingChanges: {},
        })
      );
    },
  },

  reducers: {
    [SET_AUTO_TAGGING_SPECIFICATION_VALUE]:
      createSetSettingValueReducer(section),
    [SET_AUTO_TAGGING_SPECIFICATION_FIELD_VALUE]:
      createSetProviderFieldValueReducer(section),

    [SELECT_AUTO_TAGGING_SPECIFICATION_SCHEMA]: (
      state: object,
      { payload }: { payload: SchemaPayload }
    ) => {
      return selectProviderSchema(
        state,
        section,
        payload,
        (selectedSchema: unknown) => {
          return selectedSchema;
        }
      );
    },

    [CLONE_AUTO_TAGGING_SPECIFICATION]: function (
      state: object,
      { payload }: { payload: IdPayload }
    ) {
      const id = payload.id;
      const newState = getSectionState(state, section) as SectionState;
      const items = newState.items;
      const item = items.find((i) => i.id === id);

      if (!item) {
        return state;
      }

      const newId = getNextId(newState.items);
      const newItem: SpecificationItem = {
        ...item,
        id: newId,
        name: translate('DefaultNameCopiedSpecification', { name: item.name }),
      };
      newState.items = [...items, newItem];
      newState.itemMap[newId] = newState.items.length - 1;

      return updateSectionState(state, section, newState);
    },

    [CLEAR_AUTO_TAGGING_SPECIFICATIONS]: createClearReducer(section, {
      isPopulated: false,
      error: null,
      items: [],
    }),
  },
};
