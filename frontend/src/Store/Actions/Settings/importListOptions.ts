import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

interface SettingValuePayload {
  name: string;
  value: unknown;
}

const section = 'settings.importListOptions';

export const FETCH_IMPORT_LIST_OPTIONS =
  'settings/importListOptions/fetchImportListOptions';
export const SAVE_IMPORT_LIST_OPTIONS =
  'settings/importListOptions/saveImportListOptions';
export const SET_IMPORT_LIST_OPTIONS_VALUE =
  'settings/importListOptions/setImportListOptionsValue';

export const fetchImportListOptions = createThunk(FETCH_IMPORT_LIST_OPTIONS);
export const saveImportListOptions = createThunk(SAVE_IMPORT_LIST_OPTIONS);
export const setImportListOptionsValue = createAction(
  SET_IMPORT_LIST_OPTIONS_VALUE,
  (payload: SettingValuePayload) => {
    return {
      section,
      ...payload,
    };
  }
);

export default {
  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null,
    pendingChanges: {},
    isSaving: false,
    saveError: null,
    item: {},
  },

  actionHandlers: {
    [FETCH_IMPORT_LIST_OPTIONS]: createFetchHandler(
      section,
      '/config/importlist'
    ),
    [SAVE_IMPORT_LIST_OPTIONS]: createSaveHandler(
      section,
      '/config/importlist'
    ),
  },

  reducers: {
    [SET_IMPORT_LIST_OPTIONS_VALUE]: createSetSettingValueReducer(section),
  },
};
