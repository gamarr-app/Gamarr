import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllGamesSelector from './createAllGamesSelector';

function createImportGameItemSelector() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.addGame,
    (state) => state.importGame,
    createAllGamesSelector(),
    (id, addGame, importGame, games) => {
      const item = _.find(importGame.items, { id }) || {};
      const selectedGame = item && item.selectedGame;
      const isExistingGame = !!selectedGame && _.some(games, { igdbId: selectedGame.igdbId });

      return {
        defaultMonitor: addGame.defaults.monitor,
        defaultQualityProfileId: addGame.defaults.qualityProfileId,
        ...item,
        isExistingGame
      };
    }
  );
}

export default createImportGameItemSelector;
