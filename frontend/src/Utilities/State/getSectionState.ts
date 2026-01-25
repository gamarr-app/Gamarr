import _ from 'lodash';

type State = Record<string, unknown>;

function getSectionState(
  state: State,
  section: string,
  isFullStateTree: boolean = false
): State {
  if (isFullStateTree) {
    return _.get(state, section) as State;
  }

  const [, subSection] = section.split('.');

  if (subSection) {
    return Object.assign({}, state[subSection] as State);
  }

  if (state.hasOwnProperty(section)) {
    return Object.assign({}, state[section] as State);
  }

  return Object.assign({}, state);
}

export default getSectionState;
