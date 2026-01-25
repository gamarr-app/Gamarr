import _ from 'lodash';

interface PersistedState {
  addGame?: {
    defaults?: {
      monitor?: string;
    };
  };
  discoverGame?: {
    defaults?: {
      monitor?: string;
    };
  };
  [key: string]: unknown;
}

export default function migrateMonitorToEnum(
  persistedState: PersistedState | null
): void {
  if (!persistedState) {
    return;
  }

  const addGame = _.get(persistedState, 'addGame.defaults.monitor') as
    | string
    | undefined;
  const discoverGame = _.get(
    persistedState,
    'discoverGame.defaults.monitor'
  ) as string | undefined;

  if (addGame) {
    if (addGame === 'true') {
      persistedState.addGame!.defaults!.monitor = 'gameOnly';
    }

    if (addGame === 'false') {
      persistedState.addGame!.defaults!.monitor = 'none';
    }
  }

  if (discoverGame) {
    if (discoverGame === 'true') {
      persistedState.discoverGame!.defaults!.monitor = 'gameOnly';
    }

    if (discoverGame === 'false') {
      persistedState.discoverGame!.defaults!.monitor = 'none';
    }
  }
}
