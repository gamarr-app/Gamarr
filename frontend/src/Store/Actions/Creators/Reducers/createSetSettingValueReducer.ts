import _ from 'lodash';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

type State = Record<string, unknown>;

interface SettingPayload {
  section: string;
  name: string;
  value: unknown;
}

interface Action {
  payload: SettingPayload;
}

interface Item {
  [key: string]: unknown;
}

interface PendingChanges {
  [key: string]: unknown;
}

function createSetSettingValueReducer(section: string) {
  return (state: State, { payload }: Action): State => {
    if (section === payload.section) {
      const { name, value } = payload;
      const newState = getSectionState(state, section);
      newState.pendingChanges = Object.assign(
        {},
        newState.pendingChanges as PendingChanges
      );

      const item = newState.item as Item | undefined;
      const currentValue = item ? item[name] : null;
      const pendingState = newState.pendingChanges as PendingChanges;

      let parsedValue: unknown = null;

      if (_.isNumber(currentValue) && value != null) {
        parsedValue = parseInt(value as string);
      } else {
        parsedValue = value;
      }

      if (currentValue === parsedValue) {
        delete pendingState[name];
      } else {
        pendingState[name] = parsedValue;
      }

      return updateSectionState(state, section, newState);
    }

    return state;
  };
}

export default createSetSettingValueReducer;
