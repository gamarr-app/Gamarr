import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler from 'Store/Actions/Creators/createSaveProviderHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';

//
// Variables

const section = 'settings.qualityProfiles';

//
// Actions Types

export const FETCH_QUALITY_PROFILES =
  'settings/qualityProfiles/fetchQualityProfiles';
export const FETCH_QUALITY_PROFILE_SCHEMA =
  'settings/qualityProfiles/fetchQualityProfileSchema';
export const SAVE_QUALITY_PROFILE =
  'settings/qualityProfiles/saveQualityProfile';
export const DELETE_QUALITY_PROFILE =
  'settings/qualityProfiles/deleteQualityProfile';
export const SET_QUALITY_PROFILE_VALUE =
  'settings/qualityProfiles/setQualityProfileValue';
export const CLONE_QUALITY_PROFILE =
  'settings/qualityProfiles/cloneQualityProfile';

//
// Action Creators

export const fetchQualityProfiles = createThunk(FETCH_QUALITY_PROFILES);
export const fetchQualityProfileSchema = createThunk(
  FETCH_QUALITY_PROFILE_SCHEMA
);
export const saveQualityProfile = createThunk(SAVE_QUALITY_PROFILE);
export const deleteQualityProfile = createThunk(DELETE_QUALITY_PROFILE);

export const setQualityProfileValue = createAction(
  SET_QUALITY_PROFILE_VALUE,
  (payload: { name: string; value: unknown }) => {
    return {
      section,
      ...payload,
    };
  }
);

export const cloneQualityProfile = createAction(CLONE_QUALITY_PROFILE);

//
// Details

interface QualityProfileItem {
  id: number;
  name: string;
  [key: string]: unknown;
}

type State = Record<string, unknown>;

interface QualityProfileSectionState extends State {
  items: QualityProfileItem[];
  pendingChanges: Record<string, unknown>;
}

interface CloneQualityProfilePayload {
  id: number;
}

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    isDeleting: false,
    deleteError: null as unknown,
    isSchemaFetching: false,
    isSchemaPopulated: false,
    schemaError: null as unknown,
    schema: {} as Record<string, unknown>,
    isSaving: false,
    saveError: null as unknown,
    items: [] as QualityProfileItem[],
    pendingChanges: {} as Record<string, unknown>,
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_QUALITY_PROFILES]: createFetchHandler(section, '/qualityprofile'),
    [FETCH_QUALITY_PROFILE_SCHEMA]: createFetchSchemaHandler(
      section,
      '/qualityprofile/schema'
    ),
    [SAVE_QUALITY_PROFILE]: createSaveProviderHandler(
      section,
      '/qualityprofile'
    ),
    [DELETE_QUALITY_PROFILE]: createRemoveItemHandler(
      section,
      '/qualityprofile'
    ),
  },

  //
  // Reducers

  reducers: {
    [SET_QUALITY_PROFILE_VALUE]: createSetSettingValueReducer(section),

    [CLONE_QUALITY_PROFILE]: function (
      state: State,
      { payload }: { payload: CloneQualityProfilePayload }
    ) {
      const id = payload.id;
      const newState = getSectionState(
        state,
        section
      ) as QualityProfileSectionState;
      const item = newState.items.find((i) => i.id === id);

      if (!item) {
        return state;
      }

      const pendingChanges: Record<string, unknown> = { ...item, id: 0 };
      delete pendingChanges.id;

      pendingChanges.name = translate('DefaultNameCopiedProfile', {
        name: pendingChanges.name as string,
      });
      newState.pendingChanges = pendingChanges;

      return updateSectionState(state, section, newState);
    },
  },
};
