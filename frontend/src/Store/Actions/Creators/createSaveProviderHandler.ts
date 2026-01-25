import $ from 'jquery';
import _ from 'lodash';
import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getProviderState from 'Utilities/State/getProviderState';
import { set, updateItem } from '../baseActions';

interface SaveProviderPayload {
  id?: number;
  queryParams?: Record<string, unknown>;
  [key: string]: unknown;
}

type GetState = () => AppState;

const abortCurrentRequests: Record<string, (() => void) | null> = {};
let lastSaveData: Record<string, unknown> | null = null;

export function createCancelSaveProviderHandler(section: string) {
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

function createSaveProviderHandler(section: string, url: string) {
  return function (
    getState: GetState,
    payload: SaveProviderPayload,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, isSaving: true }));

    const { id, queryParams = {}, ...otherPayload } = payload;

    const saveData = getProviderState(
      { id, ...otherPayload },
      getState as unknown as () => Record<string, unknown>,
      section
    );
    const requestUrl = id ? `${url}/${id}` : url;
    const params: Record<string, unknown> = { ...queryParams };

    if (_.isEqual(saveData, lastSaveData)) {
      params.forceSave = true;
    }

    lastSaveData = saveData;

    const ajaxOptions = {
      url: `${requestUrl}?${$.param(params, true)}`,
      method: id ? 'PUT' : 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify(saveData),
    };

    const { request, abortRequest } = createAjaxRequest(ajaxOptions);

    abortCurrentRequests[section] = abortRequest;

    request.done((data: Record<string, unknown>) => {
      lastSaveData = null;

      dispatch(
        batchActions([
          updateItem({ section, ...data }),

          set({
            section,
            isSaving: false,
            saveError: null,
            pendingChanges: {},
          }),
        ])
      );
    });

    request.fail((xhr) => {
      dispatch(
        set({
          section,
          isSaving: false,
          saveError: xhr.aborted ? null : xhr,
        })
      );
    });
  };
}

export default createSaveProviderHandler;
