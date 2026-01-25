import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler, {
  createCancelSaveProviderHandler,
} from 'Store/Actions/Creators/createSaveProviderHandler';
import createTestProviderHandler, {
  createCancelTestProviderHandler,
} from 'Store/Actions/Creators/createTestProviderHandler';
import createSetProviderFieldValueReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValueReducer';
import createSetProviderFieldValuesReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValuesReducer';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import selectProviderSchema from 'Utilities/State/selectProviderSchema';

//
// Variables

const section = 'settings.notifications';

//
// Actions Types

export const FETCH_NOTIFICATIONS = 'settings/notifications/fetchNotifications';
export const FETCH_NOTIFICATION_SCHEMA =
  'settings/notifications/fetchNotificationSchema';
export const SELECT_NOTIFICATION_SCHEMA =
  'settings/notifications/selectNotificationSchema';
export const SET_NOTIFICATION_VALUE =
  'settings/notifications/setNotificationValue';
export const SET_NOTIFICATION_FIELD_VALUE =
  'settings/notifications/setNotificationFieldValue';
export const SET_NOTIFICATION_FIELD_VALUES =
  'settings/notifications/setNotificationFieldValues';
export const SAVE_NOTIFICATION = 'settings/notifications/saveNotification';
export const CANCEL_SAVE_NOTIFICATION =
  'settings/notifications/cancelSaveNotification';
export const DELETE_NOTIFICATION = 'settings/notifications/deleteNotification';
export const TEST_NOTIFICATION = 'settings/notifications/testNotification';
export const CANCEL_TEST_NOTIFICATION =
  'settings/notifications/cancelTestNotification';

//
// Action Creators

export const fetchNotifications = createThunk(FETCH_NOTIFICATIONS);
export const fetchNotificationSchema = createThunk(FETCH_NOTIFICATION_SCHEMA);
export const selectNotificationSchema = createAction(
  SELECT_NOTIFICATION_SCHEMA
);

export const saveNotification = createThunk(SAVE_NOTIFICATION);
export const cancelSaveNotification = createThunk(CANCEL_SAVE_NOTIFICATION);
export const deleteNotification = createThunk(DELETE_NOTIFICATION);
export const testNotification = createThunk(TEST_NOTIFICATION);
export const cancelTestNotification = createThunk(CANCEL_TEST_NOTIFICATION);

export const setNotificationValue = createAction(
  SET_NOTIFICATION_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

export const setNotificationFieldValue = createAction(
  SET_NOTIFICATION_FIELD_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

export const setNotificationFieldValues = createAction(
  SET_NOTIFICATION_FIELD_VALUES,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

//
// Details

interface NotificationSchema {
  name?: string;
  implementationName?: string;
  onGrab?: boolean;
  onDownload?: boolean;
  onUpgrade?: boolean;
  onRename?: boolean;
  onGameAdded?: boolean;
  onGameDelete?: boolean;
  onGameFileDelete?: boolean;
  onGameFileDeleteForUpgrade?: boolean;
  onApplicationUpdate?: boolean;
  onManualInteractionRequired?: boolean;
  supportsOnGrab?: boolean;
  supportsOnDownload?: boolean;
  supportsOnUpgrade?: boolean;
  supportsOnRename?: boolean;
  supportsOnGameAdded?: boolean;
  supportsOnGameDelete?: boolean;
  supportsOnGameFileDelete?: boolean;
  supportsOnGameFileDeleteForUpgrade?: boolean;
  supportsOnApplicationUpdate?: boolean;
  supportsOnManualInteractionRequired?: boolean;
}

interface NotificationItem {
  id: number;
  name: string;
}

type State = Record<string, unknown>;

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
    schema: [] as NotificationSchema[],
    selectedSchema: {} as NotificationSchema,
    isSaving: false,
    saveError: null as unknown,
    isTesting: false,
    items: [] as NotificationItem[],
    pendingChanges: {} as Record<string, unknown>,
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_NOTIFICATIONS]: createFetchHandler(section, '/notification'),
    [FETCH_NOTIFICATION_SCHEMA]: createFetchSchemaHandler(
      section,
      '/notification/schema'
    ),

    [SAVE_NOTIFICATION]: createSaveProviderHandler(section, '/notification'),
    [CANCEL_SAVE_NOTIFICATION]: createCancelSaveProviderHandler(section),
    [DELETE_NOTIFICATION]: createRemoveItemHandler(section, '/notification'),
    [TEST_NOTIFICATION]: createTestProviderHandler(section, '/notification'),
    [CANCEL_TEST_NOTIFICATION]: createCancelTestProviderHandler(section),
  },

  //
  // Reducers

  reducers: {
    [SET_NOTIFICATION_VALUE]: createSetSettingValueReducer(section),
    [SET_NOTIFICATION_FIELD_VALUE]: createSetProviderFieldValueReducer(section),
    [SET_NOTIFICATION_FIELD_VALUES]:
      createSetProviderFieldValuesReducer(section),

    [SELECT_NOTIFICATION_SCHEMA]: (
      state: State,
      { payload }: { payload: { implementation: string; presetName?: string } }
    ) => {
      return selectProviderSchema(state, section, payload, (selectedSchema) => {
        const schema = selectedSchema as NotificationSchema;
        schema.name = schema.implementationName;
        schema.onGrab = schema.supportsOnGrab;
        schema.onDownload = schema.supportsOnDownload;
        schema.onUpgrade = schema.supportsOnUpgrade;
        schema.onRename = schema.supportsOnRename;
        schema.onGameAdded = schema.supportsOnGameAdded;
        schema.onGameDelete = schema.supportsOnGameDelete;
        schema.onGameFileDelete = schema.supportsOnGameFileDelete;
        schema.onGameFileDeleteForUpgrade =
          schema.supportsOnGameFileDeleteForUpgrade;
        schema.onApplicationUpdate = schema.supportsOnApplicationUpdate;
        schema.onManualInteractionRequired =
          schema.supportsOnManualInteractionRequired;

        return selectedSchema;
      });
    },
  },
};
