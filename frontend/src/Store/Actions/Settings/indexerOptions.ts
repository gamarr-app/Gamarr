import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

interface SettingValuePayload {
  name: string;
  value: unknown;
}

const section = 'settings.indexerOptions';

export const FETCH_INDEXER_OPTIONS =
  'settings/indexerOptions/fetchIndexerOptions';
export const SAVE_INDEXER_OPTIONS =
  'settings/indexerOptions/saveIndexerOptions';
export const SET_INDEXER_OPTIONS_VALUE =
  'settings/indexerOptions/setIndexerOptionsValue';

export const fetchIndexerOptions = createThunk(FETCH_INDEXER_OPTIONS);
export const saveIndexerOptions = createThunk(SAVE_INDEXER_OPTIONS);
export const setIndexerOptionsValue = createAction(
  SET_INDEXER_OPTIONS_VALUE,
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
    [FETCH_INDEXER_OPTIONS]: createFetchHandler(section, '/config/indexer'),
    [SAVE_INDEXER_OPTIONS]: createSaveHandler(section, '/config/indexer'),
  },

  reducers: {
    [SET_INDEXER_OPTIONS_VALUE]: createSetSettingValueReducer(section),
  },
};
