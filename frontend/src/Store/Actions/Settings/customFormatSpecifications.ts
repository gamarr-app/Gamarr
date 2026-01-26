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

interface CustomFormatItem {
  specifications: SpecificationItem[];
  [key: string]: unknown;
}

interface SectionState {
  items: SpecificationItem[];
  itemMap: Record<number, number>;
  [key: string]: unknown;
}

interface CustomFormatsSectionState {
  items: CustomFormatItem[];
  itemMap: Record<number, number>;
  [key: string]: unknown;
}

const section = 'settings.customFormatSpecifications';

export const FETCH_CUSTOM_FORMAT_SPECIFICATIONS =
  'settings/customFormatSpecifications/fetchCustomFormatSpecifications';
export const FETCH_CUSTOM_FORMAT_SPECIFICATION_SCHEMA =
  'settings/customFormatSpecifications/fetchCustomFormatSpecificationSchema';
export const SELECT_CUSTOM_FORMAT_SPECIFICATION_SCHEMA =
  'settings/customFormatSpecifications/selectCustomFormatSpecificationSchema';
export const SET_CUSTOM_FORMAT_SPECIFICATION_VALUE =
  'settings/customFormatSpecifications/setCustomFormatSpecificationValue';
export const SET_CUSTOM_FORMAT_SPECIFICATION_FIELD_VALUE =
  'settings/customFormatSpecifications/setCustomFormatSpecificationFieldValue';
export const SAVE_CUSTOM_FORMAT_SPECIFICATION =
  'settings/customFormatSpecifications/saveCustomFormatSpecification';
export const DELETE_CUSTOM_FORMAT_SPECIFICATION =
  'settings/customFormatSpecifications/deleteCustomFormatSpecification';
export const DELETE_ALL_CUSTOM_FORMAT_SPECIFICATION =
  'settings/customFormatSpecifications/deleteAllCustomFormatSpecification';
export const CLONE_CUSTOM_FORMAT_SPECIFICATION =
  'settings/customFormatSpecifications/cloneCustomFormatSpecification';
export const CLEAR_CUSTOM_FORMAT_SPECIFICATIONS =
  'settings/customFormatSpecifications/clearCustomFormatSpecifications';
export const CLEAR_CUSTOM_FORMAT_SPECIFICATION_PENDING =
  'settings/customFormatSpecifications/clearCustomFormatSpecificationPending';

export const fetchCustomFormatSpecifications = createThunk(
  FETCH_CUSTOM_FORMAT_SPECIFICATIONS
);
export const fetchCustomFormatSpecificationSchema = createThunk(
  FETCH_CUSTOM_FORMAT_SPECIFICATION_SCHEMA
);
export const selectCustomFormatSpecificationSchema = createAction(
  SELECT_CUSTOM_FORMAT_SPECIFICATION_SCHEMA
);

export const saveCustomFormatSpecification = createThunk(
  SAVE_CUSTOM_FORMAT_SPECIFICATION
);
export const deleteCustomFormatSpecification = createThunk(
  DELETE_CUSTOM_FORMAT_SPECIFICATION
);
export const deleteAllCustomFormatSpecification = createThunk(
  DELETE_ALL_CUSTOM_FORMAT_SPECIFICATION
);

export const setCustomFormatSpecificationValue = createAction(
  SET_CUSTOM_FORMAT_SPECIFICATION_VALUE,
  (payload: SettingValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

export const setCustomFormatSpecificationFieldValue = createAction(
  SET_CUSTOM_FORMAT_SPECIFICATION_FIELD_VALUE,
  (payload: SettingValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

export const cloneCustomFormatSpecification = createAction(
  CLONE_CUSTOM_FORMAT_SPECIFICATION
);

export const clearCustomFormatSpecification = createAction(
  CLEAR_CUSTOM_FORMAT_SPECIFICATIONS
);

export const clearCustomFormatSpecificationPending = createThunk(
  CLEAR_CUSTOM_FORMAT_SPECIFICATION_PENDING
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
    [FETCH_CUSTOM_FORMAT_SPECIFICATION_SCHEMA]: createFetchSchemaHandler(
      section,
      '/customformat/schema'
    ),

    [FETCH_CUSTOM_FORMAT_SPECIFICATIONS]: (
      getState: () => AppState,
      payload: IdPayload,
      dispatch: Dispatch
    ) => {
      let tags: SpecificationItem[] = [];
      if (payload.id) {
        const cfState = getSectionState(
          getState(),
          'settings.customFormats',
          true
        ) as CustomFormatsSectionState;
        const cf = cfState.items[cfState.itemMap[payload.id]];
        tags = cf.specifications.map((tag, i) => {
          return {
            ...tag,
            id: i + 1,
          };
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

    [SAVE_CUSTOM_FORMAT_SPECIFICATION]: (
      getState: () => AppState,
      payload: IdPayload & Record<string, unknown>,
      dispatch: Dispatch
    ) => {
      const { id, ...otherPayload } = payload;

      const saveData = getProviderState(
        { id, ...otherPayload },
        getState,
        section,
        false
      ) as SpecificationItem;

      if (!saveData.id) {
        const items = getState().settings.customFormatSpecifications.items;
        saveData.id = getNextId(items);
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

    [DELETE_CUSTOM_FORMAT_SPECIFICATION]: (
      _getState: () => AppState,
      payload: IdPayload,
      dispatch: Dispatch
    ) => {
      const id = payload.id;
      return dispatch(removeItem({ section, id }));
    },

    [DELETE_ALL_CUSTOM_FORMAT_SPECIFICATION]: (
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

    [CLEAR_CUSTOM_FORMAT_SPECIFICATION_PENDING]: (
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
    [SET_CUSTOM_FORMAT_SPECIFICATION_VALUE]:
      createSetSettingValueReducer(section),
    [SET_CUSTOM_FORMAT_SPECIFICATION_FIELD_VALUE]:
      createSetProviderFieldValueReducer(section),

    [SELECT_CUSTOM_FORMAT_SPECIFICATION_SCHEMA]: (
      state: object,
      { payload }: { payload: SchemaPayload }
    ) => {
      return selectProviderSchema(state, section, payload);
    },

    [CLONE_CUSTOM_FORMAT_SPECIFICATION]: function (
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

    [CLEAR_CUSTOM_FORMAT_SPECIFICATIONS]: createClearReducer(section, {
      isPopulated: false,
      error: null,
      items: [],
    }),
  },
};
