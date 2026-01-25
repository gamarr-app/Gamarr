import $ from 'jquery';
import _ from 'lodash';
import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getProviderState from 'Utilities/State/getProviderState';
import { set } from '../baseActions';

interface TestProviderPayload {
  queryParams?: Record<string, unknown>;
  [key: string]: unknown;
}

type GetState = () => AppState;

const abortCurrentRequests: Record<string, (() => void) | null> = {};
let lastTestData: Record<string, unknown> | null = null;

export function createCancelTestProviderHandler(section: string) {
  return function (
    _getState: GetState,
    _payload: unknown,
    _dispatch: Dispatch
  ): void {
    if (abortCurrentRequests[section]) {
      abortCurrentRequests[section]!();
      abortCurrentRequests[section] = null;
    }
  };
}

function createTestProviderHandler(section: string, url: string) {
  return function (
    getState: GetState,
    payload: TestProviderPayload,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, isTesting: true }));

    const { queryParams = {}, ...otherPayload } = payload;

    const testData = getProviderState(
      { ...otherPayload },
      getState as unknown as () => Record<string, unknown>,
      section
    );
    const params: Record<string, unknown> = { ...queryParams };

    if (_.isEqual(testData, lastTestData)) {
      params.forceTest = true;
    }

    lastTestData = testData;

    const ajaxOptions = {
      url: `${url}/test?${$.param(params, true)}`,
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify(testData),
    };

    const { request, abortRequest } = createAjaxRequest(ajaxOptions);

    abortCurrentRequests[section] = abortRequest;

    request.done(() => {
      lastTestData = null;

      dispatch(
        set({
          section,
          isTesting: false,
          saveError: null,
        })
      );
    });

    request.fail((xhr) => {
      dispatch(
        set({
          section,
          isTesting: false,
          saveError: xhr.aborted ? null : xhr,
        })
      );
    });
  };
}

export default createTestProviderHandler;
