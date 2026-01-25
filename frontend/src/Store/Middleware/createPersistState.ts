import _ from 'lodash';
import persistState from 'redux-localstorage';
import actions from 'Store/Actions';
import migrate from 'Store/Migrators/migrate';

interface Column {
  name: string;
  isVisible: boolean;
  [key: string]: unknown;
}

interface ActionModule {
  persistState?: string[];
  [key: string]: unknown;
}

type PersistedState = Record<string, unknown> | null;

const columnPaths: string[] = [];

const paths = _.reduce(
  [...actions] as ActionModule[],
  (acc: string[], action) => {
    if (action.persistState) {
      action.persistState.forEach((path) => {
        if (path.match(/\.columns$/)) {
          columnPaths.push(path);
        }

        acc.push(path);
      });
    }

    return acc;
  },
  []
);

function mergeColumns(
  path: string,
  initialState: Record<string, unknown>,
  persistedState: Record<string, unknown>,
  computedState: Record<string, unknown>
): void {
  const initialColumns = _.get(initialState, path) as Column[] | undefined;
  const persistedColumns = _.get(persistedState, path) as Column[] | undefined;

  if (!persistedColumns || !persistedColumns.length || !initialColumns) {
    return;
  }

  const columns: Column[] = [];

  // Add persisted columns in the same order they're currently in
  // as long as they haven't been removed.

  persistedColumns.forEach((persistedColumn) => {
    const column = initialColumns.find((i) => i.name === persistedColumn.name);

    if (column) {
      const newColumn: Column = { name: '', isVisible: false };

      // We can't use a spread operator or Object.assign to clone the column
      // or any accessors are lost and can break translations.
      for (const prop of Object.keys(column)) {
        Object.defineProperty(
          newColumn,
          prop,
          Object.getOwnPropertyDescriptor(column, prop)!
        );
      }

      newColumn.isVisible = persistedColumn.isVisible;

      columns.push(newColumn);
    }
  });

  // Add any columns added to the app in the initial position.
  initialColumns.forEach((initialColumn, index) => {
    const persistedColumnIndex = persistedColumns.findIndex(
      (i) => i.name === initialColumn.name
    );
    const column = Object.assign({}, initialColumn);

    if (persistedColumnIndex === -1) {
      columns.splice(index, 0, column);
    }
  });

  // Set the columns in the persisted state
  _.set(computedState, path, columns);
}

function slicer(paths_: string[]) {
  return (state: Record<string, unknown>): Record<string, unknown> => {
    const subset: Record<string, unknown> = {};

    paths_.forEach((path) => {
      _.set(subset, path, _.get(state, path));
    });

    return subset;
  };
}

function serialize(obj: unknown): string {
  return JSON.stringify(obj, null, 2);
}

function merge(
  initialState: Record<string, unknown>,
  persistedState: PersistedState
): Record<string, unknown> {
  if (!persistedState) {
    return initialState;
  }

  const computedState: Record<string, unknown> = {};

  _.merge(computedState, initialState, persistedState);

  columnPaths.forEach((columnPath) => {
    mergeColumns(
      columnPath,
      initialState,
      persistedState as Record<string, unknown>,
      computedState
    );
  });

  return computedState;
}

const KEY = 'gamarr';

const config = {
  slicer,
  serialize,
  merge,
  key: window.Gamarr.instanceName.toLowerCase().replace(/ /g, '_') || KEY,
};

export default function createPersistState() {
  // Migrate existing local storage value to new key if it does not already exist.
  // Leave old value as-is in case there are multiple instances using the same key.
  if (
    config.key !== KEY &&
    localStorage.getItem(KEY) &&
    !localStorage.getItem(config.key)
  ) {
    localStorage.setItem(config.key, localStorage.getItem(KEY)!);
  }

  // Migrate existing local storage before proceeding
  const persistedState = JSON.parse(
    localStorage.getItem(config.key) || 'null'
  ) as PersistedState;
  migrate(persistedState);
  localStorage.setItem(config.key, serialize(persistedState));

  return persistState(paths, config);
}
