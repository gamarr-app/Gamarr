import { sortDirections } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

type State = Record<string, unknown>;

interface SortPayload {
  sortKey?: string;
  sortDirection?: SortDirection;
}

interface Action {
  payload: SortPayload;
}

function createSetClientSideCollectionSortReducer(section: string) {
  return (state: State, { payload }: Action): State => {
    const newState = getSectionState(state, section);

    const sortKey = payload.sortKey || (newState.sortKey as string);
    let sortDirection = payload.sortDirection;

    if (!sortDirection) {
      if (payload.sortKey === newState.sortKey) {
        sortDirection =
          newState.sortDirection === sortDirections.ASCENDING
            ? sortDirections.DESCENDING
            : sortDirections.ASCENDING;
      } else {
        sortDirection = newState.sortDirection as SortDirection;
      }
    }

    newState.sortKey = sortKey;
    newState.sortDirection = sortDirection;

    return updateSectionState(state, section, newState);
  };
}

export default createSetClientSideCollectionSortReducer;
