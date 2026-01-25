import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

interface ProviderFieldValuesPayload {
  section: string;
  properties: Record<string, unknown>;
}

interface Action {
  payload: ProviderFieldValuesPayload;
}

interface PendingChanges {
  fields?: Record<string, unknown>;
  [key: string]: unknown;
}

function createSetProviderFieldValuesReducer(section: string) {
  return <T extends object>(state: T, { payload }: Action): T => {
    if (section === payload.section) {
      const { properties } = payload;
      const newState = getSectionState(state, section);
      newState.pendingChanges = Object.assign(
        {},
        newState.pendingChanges as PendingChanges
      );
      const pendingChanges = newState.pendingChanges as PendingChanges;
      const fields = Object.assign({}, pendingChanges.fields || {});

      Object.keys(properties).forEach((name) => {
        fields[name] = properties[name];
      });

      pendingChanges.fields = fields;

      return updateSectionState(state, section, newState);
    }

    return state;
  };
}

export default createSetProviderFieldValuesReducer;
