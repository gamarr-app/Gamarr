import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

type State = Record<string, unknown>;

interface FilterPayload {
  selectedFilterKey: string;
}

interface Action {
  payload: FilterPayload;
}

function createSetClientSideCollectionFilterReducer(section: string) {
  return (state: State, { payload }: Action): State => {
    const newState = getSectionState(state, section);

    newState.selectedFilterKey = payload.selectedFilterKey;

    return updateSectionState(state, section, newState);
  };
}

export default createSetClientSideCollectionFilterReducer;
