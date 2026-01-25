import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler from 'Store/Actions/Creators/createSaveProviderHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

//
// Variables

const section = 'settings.releaseProfiles';

//
// Actions Types

export const FETCH_RELEASE_PROFILES =
  'settings/releaseProfiles/fetchReleaseProfiles';
export const SAVE_RELEASE_PROFILE =
  'settings/releaseProfiles/saveReleaseProfile';
export const DELETE_RELEASE_PROFILE =
  'settings/releaseProfiles/deleteReleaseProfile';
export const SET_RELEASE_PROFILE_VALUE =
  'settings/releaseProfiles/setReleaseProfileValue';

//
// Action Creators

export const fetchReleaseProfiles = createThunk(FETCH_RELEASE_PROFILES);
export const saveReleaseProfile = createThunk(SAVE_RELEASE_PROFILE);
export const deleteReleaseProfile = createThunk(DELETE_RELEASE_PROFILE);

export const setReleaseProfileValue = createAction(
  SET_RELEASE_PROFILE_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

//
// Details

interface ReleaseProfileItem {
  id: number;
  name: string;
}

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    isSaving: false,
    saveError: null as unknown,
    items: [] as ReleaseProfileItem[],
    pendingChanges: {} as Record<string, unknown>,
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_RELEASE_PROFILES]: createFetchHandler(section, '/releaseprofile'),

    [SAVE_RELEASE_PROFILE]: createSaveProviderHandler(
      section,
      '/releaseprofile'
    ),

    [DELETE_RELEASE_PROFILE]: createRemoveItemHandler(
      section,
      '/releaseprofile'
    ),
  },

  //
  // Reducers

  reducers: {
    [SET_RELEASE_PROFILE_VALUE]: createSetSettingValueReducer(section),
  },
};
