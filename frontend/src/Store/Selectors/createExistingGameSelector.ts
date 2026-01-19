import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllGamesSelector from './createAllGamesSelector';

function createExistingGameSelector() {
  return createSelector(
    (_: AppState, { igdbId }: { igdbId: number }) => igdbId,
    (_: AppState, { steamAppId }: { steamAppId: number }) => steamAppId,
    createAllGamesSelector(),
    (igdbId, steamAppId, games) => {
      // Check by Steam App ID first (primary identifier)
      if (steamAppId && steamAppId > 0) {
        return games.some((game) => game.steamAppId === steamAppId);
      }

      // Fall back to IGDB ID (only if it's a valid non-zero ID)
      if (igdbId && igdbId > 0) {
        return games.some((game) => game.igdbId === igdbId);
      }

      return false;
    }
  );
}

export default createExistingGameSelector;
