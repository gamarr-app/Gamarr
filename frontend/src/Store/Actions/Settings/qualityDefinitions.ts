import _ from 'lodash';
import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { clearPendingChanges, set, update } from 'Store/Actions/baseActions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import { createThunk } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

//
// Variables

const section = 'settings.qualityDefinitions';

//
// Actions Types

export const FETCH_QUALITY_DEFINITIONS =
  'settings/qualityDefinitions/fetchQualityDefinitions';
export const SAVE_QUALITY_DEFINITIONS =
  'settings/qualityDefinitions/saveQualityDefinitions';
export const SET_QUALITY_DEFINITION_VALUE =
  'settings/qualityDefinitions/setQualityDefinitionValue';

//
// Action Creators

export const fetchQualityDefinitions = createThunk(FETCH_QUALITY_DEFINITIONS);
export const saveQualityDefinitions = createThunk(SAVE_QUALITY_DEFINITIONS);

export const setQualityDefinitionValue = createAction(
  SET_QUALITY_DEFINITION_VALUE
);

//
// Details

interface QualityDefinitionItem {
  id: number;
  [key: string]: unknown;
}

type State = Record<string, unknown>;

interface QualityDefinitionSectionState extends State {
  items: QualityDefinitionItem[];
  pendingChanges: Record<number, Record<string, unknown>>;
}

interface SetQualityDefinitionValuePayload {
  id: number;
  name: string;
  value: unknown;
}

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    items: [] as QualityDefinitionItem[],
    isSaving: false,
    saveError: null as unknown,
    pendingChanges: {} as Record<number, Record<string, unknown>>,
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_QUALITY_DEFINITIONS]: createFetchHandler(
      section,
      '/qualitydefinition'
    ),

    [SAVE_QUALITY_DEFINITIONS]: function (
      getState: () => AppState,
      _payload: unknown,
      dispatch: Dispatch
    ) {
      const qualityDefinitions = getState().settings.qualityDefinitions;

      const upatedDefinitions = Object.keys(
        qualityDefinitions.pendingChanges
      ).map((key) => {
        const id = parseInt(key);
        const pendingChanges = qualityDefinitions.pendingChanges[id] || {};
        const item = _.find(qualityDefinitions.items, { id });

        return Object.assign({}, item, pendingChanges);
      });

      // If there is nothing to save don't bother isSaving
      if (!upatedDefinitions || !upatedDefinitions.length) {
        return;
      }

      dispatch(
        set({
          section,
          isSaving: true,
        })
      );

      const promise = createAjaxRequest({
        method: 'PUT',
        url: '/qualitydefinition/update',
        data: JSON.stringify(upatedDefinitions),
        contentType: 'application/json',
        dataType: 'json',
      }).request;

      promise.done((data: unknown) => {
        dispatch(
          batchActions([
            set({
              section,
              isSaving: false,
              saveError: null,
            }),

            update({ section, data }),
            clearPendingChanges({ section }),
          ])
        );
      });

      promise.fail((xhr: unknown) => {
        dispatch(
          set({
            section,
            isSaving: false,
            saveError: xhr,
          })
        );
      });
    },
  },

  //
  // Reducers

  reducers: {
    [SET_QUALITY_DEFINITION_VALUE]: function (
      state: State,
      { payload }: { payload: SetQualityDefinitionValuePayload }
    ) {
      const { id, name, value } = payload;
      const newState = getSectionState(
        state,
        section
      ) as QualityDefinitionSectionState;
      newState.pendingChanges = _.cloneDeep(newState.pendingChanges);

      const pendingState = newState.pendingChanges[id] || {};
      const item = _.find(newState.items, { id });
      const currentValue = item ? item[name] : undefined;

      if (currentValue === value) {
        delete pendingState[name];
      } else {
        pendingState[name] = value;
      }

      if (_.isEmpty(pendingState)) {
        delete newState.pendingChanges[id];
      } else {
        newState.pendingChanges[id] = pendingState;
      }

      return updateSectionState(state, section, newState);
    },
  },
};
