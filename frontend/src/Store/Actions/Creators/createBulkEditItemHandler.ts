import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, updateItem } from '../baseActions';

interface ProviderItem {
  id: number;
  [key: string]: unknown;
}

type GetState = () => AppState;

function createBulkEditItemHandler(section: string, url: string) {
  return function (_getState: GetState, payload: unknown, dispatch: Dispatch) {
    dispatch(set({ section, isSaving: true }));

    const ajaxOptions = {
      url: `${url}`,
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json',
    };

    const promise = createAjaxRequest(ajaxOptions).request;

    promise.done((data: ProviderItem[]) => {
      dispatch(
        batchActions([
          set({
            section,
            isSaving: false,
            saveError: null,
          }),

          ...data.map((provider) => {
            return updateItem({
              section,
              ...provider,
            });
          }),
        ])
      );
    });

    promise.fail((xhr) => {
      dispatch(
        set({
          section,
          isSaving: false,
          saveError: xhr,
        })
      );
    });

    return promise;
  };
}

export default createBulkEditItemHandler;
