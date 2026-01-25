import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

function createClearReducer(section: string, defaultState: object) {
  return <T extends object>(state: T): T => {
    const newState = Object.assign(
      getSectionState(state, section),
      defaultState
    );

    return updateSectionState(state, section, newState);
  };
}

export default createClearReducer;
