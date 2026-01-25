import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

interface SettingValuePayload {
  name: string;
  value: unknown;
}

const section = 'settings.downloadClientOptions';

export const FETCH_DOWNLOAD_CLIENT_OPTIONS = 'FETCH_DOWNLOAD_CLIENT_OPTIONS';
export const SET_DOWNLOAD_CLIENT_OPTIONS_VALUE =
  'SET_DOWNLOAD_CLIENT_OPTIONS_VALUE';
export const SAVE_DOWNLOAD_CLIENT_OPTIONS = 'SAVE_DOWNLOAD_CLIENT_OPTIONS';

export const fetchDownloadClientOptions = createThunk(
  FETCH_DOWNLOAD_CLIENT_OPTIONS
);
export const saveDownloadClientOptions = createThunk(
  SAVE_DOWNLOAD_CLIENT_OPTIONS
);
export const setDownloadClientOptionsValue = createAction(
  SET_DOWNLOAD_CLIENT_OPTIONS_VALUE,
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
    [FETCH_DOWNLOAD_CLIENT_OPTIONS]: createFetchHandler(
      section,
      '/config/downloadclient'
    ),
    [SAVE_DOWNLOAD_CLIENT_OPTIONS]: createSaveHandler(
      section,
      '/config/downloadclient'
    ),
  },

  reducers: {
    [SET_DOWNLOAD_CLIENT_OPTIONS_VALUE]: createSetSettingValueReducer(section),
  },
};
