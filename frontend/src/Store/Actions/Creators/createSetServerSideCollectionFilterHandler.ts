import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import { set } from '../baseActions';

interface FilterPayload {
  [key: string]: unknown;
}

type GetState = () => AppState;
type FetchHandler = (payload?: { page: number }) => unknown;

function createSetServerSideCollectionFilterHandler(
  section: string,
  fetchHandler: FetchHandler
) {
  return function (
    _getState: GetState,
    payload: FilterPayload,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, ...payload }));
    dispatch(fetchHandler({ page: 1 }) as never);
  };
}

export default createSetServerSideCollectionFilterHandler;
