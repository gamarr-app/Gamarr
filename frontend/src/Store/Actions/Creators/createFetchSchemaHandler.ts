import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set } from '../baseActions';

type GetState = () => AppState;

function createFetchSchemaHandler(section: string, url: string) {
  return function (
    _getState: GetState,
    _payload: unknown,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, isSchemaFetching: true }));

    const promise = createAjaxRequest({
      url,
    }).request;

    promise.done((data: unknown) => {
      dispatch(
        set({
          section,
          isSchemaFetching: false,
          isSchemaPopulated: true,
          schemaError: null,
          schema: data,
        })
      );
    });

    promise.fail((xhr) => {
      dispatch(
        set({
          section,
          isSchemaFetching: false,
          isSchemaPopulated: true,
          schemaError: xhr,
        })
      );
    });
  };
}

export default createFetchSchemaHandler;
