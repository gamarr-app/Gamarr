import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';

function createMultiGamesSelector(gameIds: number[]) {
  return createSelector(
    (state: AppState) => state.games.itemMap,
    (state: AppState) => state.games.items,
    (itemMap, allGames) => {
      return gameIds.reduce((acc: Game[], gameId) => {
        const game = allGames[itemMap[gameId]];

        if (game) {
          acc.push(game);
        }

        return acc;
      }, []);
    }
  );
}

export default createMultiGamesSelector;
