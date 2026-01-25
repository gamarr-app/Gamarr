import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

interface FilterPayload {
  selectedFilterKey: string;
}

interface Action {
  payload: FilterPayload;
}

function createSetClientSideCollectionFilterReducer(section: string) {
  return <T extends object>(state: T, { payload }: Action): T => {
    const newState = getSectionState(state, section);

    newState.selectedFilterKey = payload.selectedFilterKey;

    return updateSectionState(state, section, newState);
  };
}

export default createSetClientSideCollectionFilterReducer;
