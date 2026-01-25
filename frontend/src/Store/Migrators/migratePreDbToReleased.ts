import _ from 'lodash';

interface PersistedState {
  addGame?: {
    defaults?: {
      minimumAvailability?: string;
    };
  };
  discoverGame?: {
    defaults?: {
      minimumAvailability?: string;
    };
  };
  [key: string]: unknown;
}

export default function migratePreDbToReleased(
  persistedState: PersistedState | null
): void {
  if (!persistedState) {
    return;
  }

  const addGame = _.get(
    persistedState,
    'addGame.defaults.minimumAvailability'
  ) as string | undefined;
  const discoverGame = _.get(
    persistedState,
    'discoverGame.defaults.minimumAvailability'
  ) as string | undefined;

  if (!addGame && !discoverGame) {
    return;
  }

  if (addGame === 'preDB') {
    persistedState.addGame!.defaults!.minimumAvailability = 'released';
  }

  if (discoverGame === 'preDB') {
    persistedState.discoverGame!.defaults!.minimumAvailability = 'released';
  }
}
