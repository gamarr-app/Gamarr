import _ from 'lodash';
import AppState from 'App/State/AppState';

function getSectionState<T = Record<string, unknown>>(
  state: AppState | object,
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
