import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';
import migrateMonitorToEnum from './migrateMonitorToEnum';
import migratePreDbToReleased from './migratePreDbToReleased';

type PersistedState = Record<string, unknown> | null;

export default function migrate(persistedState: PersistedState): void {
  migrateBlacklistToBlocklist(persistedState);
  migratePreDbToReleased(persistedState);
  migrateMonitorToEnum(persistedState);
}
