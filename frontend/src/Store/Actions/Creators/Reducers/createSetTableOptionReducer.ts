import _ from 'lodash';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

interface TableOptionPayload {
  pageSize?: number;
  columns?: unknown[];
  tableOptions?: Record<string, unknown>;
}

interface Action {
  payload: TableOptionPayload;
}

const whitelistedProperties: (keyof TableOptionPayload)[] = [
  'pageSize',
  'columns',
  'tableOptions',
];

function createSetTableOptionReducer(section: string) {
  return <T extends object>(state: T, { payload }: Action): T => {
    const newState = Object.assign(
      getSectionState(state, section),
      _.pick(payload, whitelistedProperties)
    );

    return updateSectionState(state, section, newState);
  };
}

export default createSetTableOptionReducer;
