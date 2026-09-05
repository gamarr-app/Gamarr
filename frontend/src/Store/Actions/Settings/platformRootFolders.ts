import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler from 'Store/Actions/Creators/createSaveProviderHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

//
// Variables

const section = 'settings.platformRootFolders';

//
// Actions Types

export const FETCH_PLATFORM_ROOT_FOLDERS =
  'settings/platformRootFolders/fetchPlatformRootFolders';
export const SAVE_PLATFORM_ROOT_FOLDER =
  'settings/platformRootFolders/savePlatformRootFolder';
export const DELETE_PLATFORM_ROOT_FOLDER =
  'settings/platformRootFolders/deletePlatformRootFolder';
export const SET_PLATFORM_ROOT_FOLDER_VALUE =
  'settings/platformRootFolders/setPlatformRootFolderValue';

//
// Action Creators

export const fetchPlatformRootFolders = createThunk(
  FETCH_PLATFORM_ROOT_FOLDERS
);
export const savePlatformRootFolder = createThunk(SAVE_PLATFORM_ROOT_FOLDER);
export const deletePlatformRootFolder = createThunk(
  DELETE_PLATFORM_ROOT_FOLDER
);

export const setPlatformRootFolderValue = createAction(
  SET_PLATFORM_ROOT_FOLDER_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

//
// Details

export interface PlatformRootFolderItem {
  id: number;
  platform: string;
  path: string;
}

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    items: [] as PlatformRootFolderItem[],
    isSaving: false,
    saveError: null as unknown,
    pendingChanges: {} as Record<string, unknown>,
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_PLATFORM_ROOT_FOLDERS]: createFetchHandler(
      section,
      '/platformrootfolder'
    ),
    [SAVE_PLATFORM_ROOT_FOLDER]: createSaveProviderHandler(
      section,
      '/platformrootfolder'
    ),
    [DELETE_PLATFORM_ROOT_FOLDER]: createRemoveItemHandler(
      section,
      '/platformrootfolder'
    ),
  },

  //
  // Reducers

  reducers: {
    [SET_PLATFORM_ROOT_FOLDER_VALUE]: createSetSettingValueReducer(section),
  },
};
