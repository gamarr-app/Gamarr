import { get } from 'lodash';

type PersistedState = Record<string, unknown> | null;

export default function migrateBlacklistToBlocklist(
  persistedState: PersistedState
): void {
  if (!persistedState) {
    return;
  }

  const blocklist = get(persistedState, 'blacklist');

  if (!blocklist) {
    return;
  }

  persistedState.blocklist = blocklist;
  delete persistedState.blacklist;
}
