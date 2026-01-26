import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import { sortDirections } from 'Helpers/Props';
import getSectionState from 'Utilities/State/getSectionState';
import { set } from '../baseActions';

interface SectionState {
  sortKey?: string;
  sortDirection?: string;
  [key: string]: unknown;
}

interface SortPayload {
  sortKey?: string;
  sortDirection?: string;
}

type GetState = () => AppState;
type FetchHandler = () => unknown;

function createSetServerSideCollectionSortHandler(
  section: string,
  fetchHandler: FetchHandler
) {
  return function (
    getState: GetState,
    payload: SortPayload,
    dispatch: Dispatch
  ): void {
    const sectionState = getSectionState<SectionState>(
      getState(),
      section,
      true
    );
    const sortKey = payload.sortKey || sectionState.sortKey;
    let sortDirection = payload.sortDirection;

    if (!sortDirection) {
      if (payload.sortKey === sectionState.sortKey) {
        sortDirection =
          sectionState.sortDirection === sortDirections.ASCENDING
            ? sortDirections.DESCENDING
            : sortDirections.ASCENDING;
      } else {
        sortDirection = sectionState.sortDirection;
      }
    }

    dispatch(set({ section, sortKey, sortDirection }));
    dispatch(fetchHandler() as never);
  };
}

export default createSetServerSideCollectionSortHandler;
