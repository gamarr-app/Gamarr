import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

interface SettingValuePayload {
  name: string;
  value: unknown;
}

const section = 'settings.general';

export const FETCH_GENERAL_SETTINGS = 'settings/general/fetchGeneralSettings';
export const SET_GENERAL_SETTINGS_VALUE =
  'settings/general/setGeneralSettingsValue';
export const SAVE_GENERAL_SETTINGS = 'settings/general/saveGeneralSettings';

export const fetchGeneralSettings = createThunk(FETCH_GENERAL_SETTINGS);
export const saveGeneralSettings = createThunk(SAVE_GENERAL_SETTINGS);
export const setGeneralSettingsValue = createAction(
  SET_GENERAL_SETTINGS_VALUE,
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
    [FETCH_GENERAL_SETTINGS]: createFetchHandler(section, '/config/host'),
    [SAVE_GENERAL_SETTINGS]: createSaveHandler(section, '/config/host'),
  },

  reducers: {
    [SET_GENERAL_SETTINGS_VALUE]: createSetSettingValueReducer(section),
  },
};
