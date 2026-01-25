import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { set, update } from 'Store/Actions/baseActions';
import { createThunk } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';

//
// Variables

const section = 'settings.namingExamples';

//
// Actions Types

export const FETCH_NAMING_EXAMPLES =
  'settings/namingExamples/fetchNamingExamples';

//
// Action Creators

export const fetchNamingExamples = createThunk(FETCH_NAMING_EXAMPLES);

//
// Details

export default {
  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null as unknown,
    item: {} as Record<string, unknown>,
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_NAMING_EXAMPLES]: function (
      getState: () => AppState,
      _payload: unknown,
      dispatch: Dispatch
    ) {
      dispatch(set({ section, isFetching: true }));

      const naming = getState().settings.naming;

      const promise = createAjaxRequest({
        url: '/config/naming/examples',
        data: Object.assign({}, naming.item, naming.pendingChanges),
      }).request;

      promise.done((data: unknown) => {
        dispatch(
          batchActions([
            update({ section, data }),

            set({
              section,
              isFetching: false,
              isPopulated: true,
              error: null,
            }),
          ])
        );
      });

      promise.fail((xhr: unknown) => {
        dispatch(
          set({
            section,
            isFetching: false,
            isPopulated: false,
            error: xhr,
          })
        );
      });
    },
  },

  //
  // Reducers

  reducers: {},
};
