import _ from 'lodash';
import { createSelector } from 'reselect';
import gameEntities from 'Game/gameEntities';

export function createGameSelectorForHook(gameId) {
  return createSelector(
    (state) => state.games.itemMap,
    (state) => state.games.items,
    (itemMap, allGames) => {

      return gameId ? allGames[itemMap[gameId]]: undefined;
    }
  );
}

export function createGameByEntitySelector() {
  return createSelector(
    (state, { gameId }) => gameId,
    (state, { gameEntity = gameEntities.GAMES }) => _.get(state, gameEntity, { items: [] }),
    (gameId, games) => {
      return _.find(games.items, { id: gameId });
    }
  );
}

function createGameSelector() {
  return createSelector(
    (state, { gameId }) => gameId,
    (state) => state.games.itemMap,
    (state) => state.games.items,
    (gameId, itemMap, allGames) => {
      return allGames[itemMap[gameId]];
    }
  );
}

export default createGameSelector;
