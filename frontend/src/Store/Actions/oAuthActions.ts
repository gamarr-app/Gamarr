/* global JQuery */
import $ from 'jquery';
import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { set } from 'Store/Actions/baseActions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest, {
  AjaxOptions as CreateAjaxOptions,
} from 'Utilities/createAjaxRequest';
import requestAction from 'Utilities/requestAction';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';
import createHandleActions from './Creators/createHandleActions';

declare global {
  interface Window {
    onCompleteOauth?: (query: string, onComplete: () => void) => void;
  }
}

interface ProviderDataValue {
  value: unknown;
}

interface ProviderData {
  fields?: unknown;
  [key: string]: ProviderDataValue | unknown;
}

interface OAuthPayload {
  name: string;
  section: string;
  provider: string;
  providerData: ProviderData;
}

interface OAuthValuePayload {
  authorizing?: boolean;
  result?: unknown;
  error?: unknown;
}

interface QueryParams {
  [key: string]: string;
}

interface OAuthResponse {
  oauthUrl?: string;
  [key: string]: string | number | boolean | undefined;
}

type OAuthResponseRecord = Record<string, string | number | boolean>;

//
// Variables

export const section = 'oAuth';
const callbackUrl = `${window.location.origin}${window.Gamarr.urlBase}/oauth.html`;

//
// State

export const defaultState = {
  authorizing: false,
  result: null as unknown,
  error: null as unknown,
};

//
// Actions Types

export const START_OAUTH = 'oAuth/startOAuth';
export const SET_OAUTH_VALUE = 'oAuth/setOAuthValue';
export const RESET_OAUTH = 'oAuth/resetOAuth';

//
// Action Creators

export const startOAuth = createThunk(START_OAUTH);
export const setOAuthValue = createAction(SET_OAUTH_VALUE);
export const resetOAuth = createAction(RESET_OAUTH);

//
// Helpers

function showOAuthWindow(
  url: string,
  payload: OAuthPayload
): JQuery.Promise<QueryParams> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const deferred: any = $.Deferred<QueryParams>();
  const selfWindow = window;

  const newWindow = window.open(url);

  if (
    !newWindow ||
    newWindow.closed ||
    typeof newWindow.closed == 'undefined'
  ) {
    // A fake validation error to mimic a 400 response from the API.
    const error = {
      status: 400,
      responseJSON: [
        {
          propertyName: payload.name,
          errorMessage: translate('OAuthPopupMessage'),
        },
      ],
    };

    return deferred.reject(error).promise();
  }

  selfWindow.onCompleteOauth = function (
    query: string,
    onComplete: () => void
  ) {
    delete selfWindow.onCompleteOauth;

    const queryParams: QueryParams = {};
    const splitQuery = query.substring(1).split('&');

    splitQuery.forEach((param) => {
      if (param) {
        const paramSplit = param.split('=');

        queryParams[paramSplit[0]] = paramSplit[1];
      }
    });

    onComplete();
    deferred.resolve(queryParams);
  };

  return deferred.promise();
}

function executeIntermediateRequest(
  payload: { provider: string; providerData: ProviderData },
  ajaxOptions: CreateAjaxOptions
): JQuery.Promise<OAuthResponse> {
  return createAjaxRequest(ajaxOptions).request.then((data: OAuthResponse) => {
    return requestAction({
      action: 'continueOAuth',
      queryParams: {
        ...data,
        callbackUrl,
      },
      ...payload,
    });
  });
}

//
// Action Handlers

export const actionHandlers = handleThunks({
  [START_OAUTH]: function (
    _getState: () => AppState,
    payload: OAuthPayload,
    dispatch: Dispatch
  ) {
    const { name, section: actionSection, ...otherPayload } = payload;

    const actionPayload = {
      action: 'startOAuth',
      queryParams: { callbackUrl },
      ...otherPayload,
    };

    dispatch(
      setOAuthValue({
        authorizing: true,
      })
    );

    let startResponse: OAuthResponse = {};

    const promise = requestAction(actionPayload)
      .then((response: OAuthResponse) => {
        startResponse = response;

        if (response.oauthUrl) {
          return showOAuthWindow(response.oauthUrl, payload);
        }

        return executeIntermediateRequest(
          otherPayload,
          response as unknown as CreateAjaxOptions
        ).then((intermediateResponse) => {
          startResponse = intermediateResponse;

          return showOAuthWindow(intermediateResponse.oauthUrl!, payload);
        });
      })
      .then((queryParams: QueryParams) => {
        // Filter out undefined values from startResponse
        const filteredStartResponse = Object.fromEntries(
          Object.entries(startResponse).filter(
            ([, v]) => v !== undefined
          )
        ) as OAuthResponseRecord;

        return requestAction({
          action: 'getOAuthToken',
          queryParams: {
            ...filteredStartResponse,
            ...queryParams,
          },
          ...otherPayload,
        });
      })
      .then((response: unknown) => {
        dispatch(
          setOAuthValue({
            authorizing: false,
            result: response,
            error: null,
          })
        );
      });

    promise.done(() => {
      // Clear any previously set save error.
      dispatch(
        set({
          section: actionSection,
          saveError: null,
        })
      );
    });

    promise.fail((xhr: { status: number }) => {
      const actions = [
        setOAuthValue({
          authorizing: false,
          result: null,
          error: xhr,
        }),
      ];

      if (xhr.status === 400) {
        // Set a save error so the UI can display validation errors to the user.
        actions.splice(
          0,
          0,
          set({
            section: actionSection,
            saveError: xhr,
          })
        );
      }

      dispatch(batchActions(actions));
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [SET_OAUTH_VALUE]: function (
      state: object,
      { payload }: { payload: OAuthValuePayload }
    ) {
      const newState = Object.assign(getSectionState(state, section), payload);

      return updateSectionState(state, section, newState);
    },

    [RESET_OAUTH]: function (state: object) {
      return updateSectionState(state, section, defaultState);
    },
  },
  defaultState,
  section
);
