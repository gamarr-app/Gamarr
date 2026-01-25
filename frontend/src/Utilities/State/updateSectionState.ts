function updateSectionState<T extends object>(
  state: T,
  section: string,
  newState: object
): T {
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
