import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { removeItem, set } from '../baseActions';

interface BulkRemovePayload {
  ids: number[];
  [key: string]: unknown;
}

type GetState = () => AppState;

function createBulkRemoveItemHandler(section: string, url: string) {
  return function (
    _getState: GetState,
    payload: BulkRemovePayload,
    dispatch: Dispatch
  ) {
    const { ids } = payload;

    dispatch(set({ section, isDeleting: true }));

    const ajaxOptions = {
      url: `${url}`,
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json',
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

          ...ids.map((id) => {
            return removeItem({ section, id });
          }),
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

export default createBulkRemoveItemHandler;
