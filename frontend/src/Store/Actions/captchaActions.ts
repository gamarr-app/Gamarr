import { Dispatch } from 'redux';
import { createAction } from 'redux-actions';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import requestAction from 'Utilities/requestAction';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'captcha';

//
// State

export interface CaptchaState {
  refreshing: boolean;
  token: string | null;
  siteKey: string | null;
  secretToken: string | null;
  ray: string | null;
  stoken: string | null;
  responseUrl: string | null;
}

export const defaultState: CaptchaState = {
  refreshing: false,
  token: null,
  siteKey: null,
  secretToken: null,
  ray: null,
  stoken: null,
  responseUrl: null,
};

//
// Actions Types

export const REFRESH_CAPTCHA = 'captcha/refreshCaptcha';
export const GET_CAPTCHA_COOKIE = 'captcha/getCaptchaCookie';
export const SET_CAPTCHA_VALUE = 'captcha/setCaptchaValue';
export const RESET_CAPTCHA = 'captcha/resetCaptcha';

//
// Action Creators

interface RefreshCaptchaPayload {
  provider: string;
  providerData: {
    fields?: unknown;
    [key: string]: { value: unknown } | unknown;
  };
  [key: string]: unknown;
}

interface GetCaptchaCookiePayload {
  provider: string;
  providerData: {
    fields?: unknown;
    [key: string]: { value: unknown } | unknown;
  };
  captchaResponse: string;
  [key: string]: unknown;
}

export const refreshCaptcha = createThunk(REFRESH_CAPTCHA);
export const getCaptchaCookie = createThunk(GET_CAPTCHA_COOKIE);
export const setCaptchaValue =
  createAction<Partial<CaptchaState>>(SET_CAPTCHA_VALUE);
export const resetCaptcha = createAction(RESET_CAPTCHA);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [REFRESH_CAPTCHA]: function (
    _getState: () => AppState,
    payload: RefreshCaptchaPayload,
    dispatch: Dispatch
  ) {
    const actionPayload = {
      action: 'checkCaptcha',
      ...payload,
    };

    dispatch(
      setCaptchaValue({
        refreshing: true,
      })
    );

    const promise = requestAction(actionPayload);

    promise.done((data: { captchaRequest?: Partial<CaptchaState> }) => {
      if (!data.captchaRequest) {
        dispatch(
          setCaptchaValue({
            refreshing: false,
          })
        );
      }

      dispatch(
        setCaptchaValue({
          refreshing: false,
          ...data.captchaRequest,
        })
      );
    });

    promise.fail(() => {
      dispatch(
        setCaptchaValue({
          refreshing: false,
        })
      );
    });
  },

  [GET_CAPTCHA_COOKIE]: function (
    getState: () => AppState,
    payload: GetCaptchaCookiePayload,
    dispatch: Dispatch
  ) {
    const state = (getState() as unknown as { captcha: CaptchaState }).captcha;

    const queryParams: Record<string, string | number | boolean> = {
      captchaResponse: payload.captchaResponse,
    };

    if (state.responseUrl) {
      queryParams.responseUrl = state.responseUrl;
    }

    if (state.ray) {
      queryParams.ray = state.ray;
    }

    const actionPayload = {
      action: 'getCaptchaCookie',
      queryParams,
      ...payload,
    };

    const promise = requestAction(actionPayload);

    promise.done((data: { captchaToken: string }) => {
      dispatch(
        setCaptchaValue({
          token: data.captchaToken,
        })
      );
    });
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [SET_CAPTCHA_VALUE]: function (
      state: CaptchaState,
      { payload }: { payload: Partial<CaptchaState> }
    ) {
      const newState = Object.assign(getSectionState(state, section), payload);

      return updateSectionState(state, section, newState);
    },

    [RESET_CAPTCHA]: function (state: CaptchaState) {
      return updateSectionState(state, section, defaultState);
    },
  },
  defaultState,
  section
);
