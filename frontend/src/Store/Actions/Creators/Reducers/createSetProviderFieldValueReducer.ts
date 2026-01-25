import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

interface ProviderFieldPayload {
  section: string;
  name: string;
  value: unknown;
}

interface Action {
  payload: ProviderFieldPayload;
}

interface PendingChanges {
  fields?: Record<string, unknown>;
  [key: string]: unknown;
}

function createSetProviderFieldValueReducer(section: string) {
  return <T extends object>(state: T, { payload }: Action): T => {
    if (section === payload.section) {
      const { name, value } = payload;
      const newState = getSectionState(state, section);
      newState.pendingChanges = Object.assign(
        {},
        newState.pendingChanges as PendingChanges
      );
      const pendingChanges = newState.pendingChanges as PendingChanges;
      const fields = Object.assign({}, pendingChanges.fields || {});

      fields[name] = value;

      pendingChanges.fields = fields;

      return updateSectionState(state, section, newState);
    }

    return state;
  };
}

export default createSetProviderFieldValueReducer;
