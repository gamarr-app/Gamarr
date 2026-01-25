type State = Record<string, unknown>;

function updateSectionState(
  state: State,
  section: string,
  newState: State
): State {
  const [, subSection] = section.split('.');

  if (subSection) {
    return Object.assign({}, state, { [subSection]: newState });
  }

  if (state.hasOwnProperty(section)) {
    return Object.assign({}, state, { [section]: newState });
  }

  return Object.assign({}, state, newState);
}

export default updateSectionState;
