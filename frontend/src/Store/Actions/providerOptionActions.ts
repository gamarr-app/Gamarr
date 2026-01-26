import _ from 'lodash';
import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import requestAction from 'Utilities/requestAction';
import updateSectionState from 'Utilities/State/updateSectionState';
import { set } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

export const section = 'providerOptions';

interface LastAction {
  actionId: number;
  payload: FetchOptionsPayload;
}

const lastActions: Record<string, LastAction | null> = {};
let lastActionId = 0;

export interface ProviderOptionsState {
  items: unknown[];
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
}

interface ProviderData {
  fields?: unknown;
  [key: string]: { value: unknown } | unknown;
}

interface FetchOptionsPayload {
  section: string;
  provider: string;
  action: string;
  providerData: ProviderData;
  [key: string]: unknown;
}

interface ClearOptionsPayload {
  section: string;
}

interface OptionsResponse {
  options?: unknown[];
}

export const defaultState: ProviderOptionsState = {
  items: [],
  isFetching: false,
  isPopulated: false,
  error: false,
};

export const FETCH_OPTIONS = 'providers/fetchOptions';
export const CLEAR_OPTIONS = 'providers/clearOptions';

export const fetchOptions = createThunk(FETCH_OPTIONS);
export const clearOptions = createAction(CLEAR_OPTIONS);

export const actionHandlers = handleThunks({
  [FETCH_OPTIONS]: function (
    getState: () => AppState,
    payload: FetchOptionsPayload,
    dispatch: Dispatch
  ) {
    const subsection = `${section}.${payload.section}`;

    if (
      lastActions[payload.section] &&
      _.isEqual(payload, lastActions[payload.section]?.payload)
    ) {
      return;
    }

    const actionId = ++lastActionId;

    lastActions[payload.section] = {
      actionId,
      payload,
    };

    const providerOptionsState = getState().providerOptions;
    const subsectionExists =
      payload.section in providerOptionsState &&
      providerOptionsState[
        payload.section as keyof typeof providerOptionsState
      ];
    if (subsectionExists) {
      dispatch(
        set({
          section: subsection,
          isFetching: true,
        })
      );
    } else {
      dispatch(
        set({
          section: subsection,
          ...defaultState,
          isFetching: true,
        })
      );
    }

    const promise = requestAction(payload);

    promise.done((data: OptionsResponse) => {
      if (lastActions[payload.section]) {
        if (lastActions[payload.section]?.actionId === actionId) {
          lastActions[payload.section] = null;
        }

        dispatch(
          set({
            section: subsection,
            isFetching: false,
            isPopulated: true,
            error: null,
            items: data.options || [],
          })
        );
      }
    });

    promise.fail((xhr: unknown) => {
      if (lastActions[payload.section]) {
        if (lastActions[payload.section]?.actionId === actionId) {
          lastActions[payload.section] = null;
        }

        dispatch(
          set({
            section: subsection,
            isFetching: false,
            isPopulated: false,
            error: xhr,
          })
        );
      }
    });
  },
});

export const reducers = createHandleActions(
  {
    [CLEAR_OPTIONS]: function (
      state: Record<string, unknown>,
      { payload }: { payload: ClearOptionsPayload }
    ) {
      const subsection = `${section}.${payload.section}`;

      lastActions[payload.section] = null;

      return updateSectionState(state, subsection, defaultState);
    },
  },
  {},
  section
);
