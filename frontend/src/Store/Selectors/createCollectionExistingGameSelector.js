import { createSelector } from 'reselect';
import createAllGamesSelector from './createAllGamesSelector';

function createCollectionExistingGameSelector() {
  return createSelector(
    (state, { igdbId }) => igdbId,
    createAllGamesSelector(),
    (igdbId, allGames) => {
      return allGames.find((game) => game.igdbId === igdbId);
    }
  );
}

export default createCollectionExistingGameSelector;
