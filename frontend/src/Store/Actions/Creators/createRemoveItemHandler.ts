import $ from 'jquery';
import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { removeItem, set } from '../baseActions';

interface RemovePayload {
  id: number;
  queryParams?: Record<string, unknown>;
}

type GetState = () => AppState;

function createRemoveItemHandler(section: string, url: string) {
  return function (
    _getState: GetState,
    payload: RemovePayload,
    dispatch: Dispatch
  ) {
    const { id, queryParams = {} } = payload;

    dispatch(set({ section, isDeleting: true }));

    const ajaxOptions = {
      url: `${url}/${id}?${$.param(queryParams, true)}`,
      method: 'DELETE',
    };

    const promise = createAjaxRequest(ajaxOptions).request;

    promise.done(() => {
      dispatch(
        batchActions([
          set({
            section,
            isDeleting: false,
            deleteError: null,
          }),

          removeItem({ section, id }),
        ])
      );
    });

    promise.fail((xhr) => {
      dispatch(
        set({
          section,
          isDeleting: false,
          deleteError: xhr,
        })
      );
    });

    return promise;
  };
}

export default createRemoveItemHandler;
