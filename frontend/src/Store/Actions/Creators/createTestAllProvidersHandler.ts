import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set } from '../baseActions';

type GetState = () => AppState;

function createTestAllProvidersHandler(section: string, url: string) {
  return function (
    _getState: GetState,
    _payload: unknown,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, isTestingAll: true }));

    const ajaxOptions = {
      url: `${url}/testall`,
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
    };

    const { request } = createAjaxRequest(ajaxOptions);

    request.done(() => {
      dispatch(
        set({
          section,
          isTestingAll: false,
          saveError: null,
        })
      );
    });

    request.fail(() => {
      dispatch(
        set({
          section,
          isTestingAll: false,
        })
      );
    });
  };
}

export default createTestAllProvidersHandler;
