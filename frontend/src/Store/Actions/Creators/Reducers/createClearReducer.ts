import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

type State = Record<string, unknown>;

function createClearReducer(section: string, defaultState: State) {
  return (state: State): State => {
    const newState = Object.assign(
      getSectionState(state, section),
      defaultState
    );

    return updateSectionState(state, section, newState);
  };
}

export default createClearReducer;
