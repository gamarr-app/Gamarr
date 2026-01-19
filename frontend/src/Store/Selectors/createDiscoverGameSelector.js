import { createSelector } from 'reselect';

function createDiscoverGameSelector() {
  return createSelector(
    (state, { gameId }) => gameId,
    (state) => state.discoverGame,
    (gameId, allGames) => {
      return allGames.items.find((game) => game.igdbId === gameId);
    }
  );
}

export default createDiscoverGameSelector;
