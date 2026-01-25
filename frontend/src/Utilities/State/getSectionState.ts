import _ from 'lodash';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function getSectionState<T = Record<string, unknown>>(
  state: object,
  section: string,
  isFullStateTree: boolean = false
): T {
  if (isFullStateTree) {
    return _.get(state, section) as T;
  }

  const [, subSection] = section.split('.');

  if (subSection) {
    return Object.assign({}, _.get(state, subSection)) as T;
  }

  if (_.has(state, section)) {
    return Object.assign({}, _.get(state, section)) as T;
  }

  return Object.assign({}, state) as T;
}

export default getSectionState;
