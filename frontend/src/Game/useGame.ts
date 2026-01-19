import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export type GameEntity =
  | 'calendar'
  | 'games'
  | 'interactiveImport.games'
  | 'wanted.cutoffUnmet'
  | 'wanted.missing';

export function createGameSelector(gameId?: number) {
  return createSelector(
    (state: AppState) => state.games.itemMap,
    (state: AppState) => state.games.items,
    (itemMap, allGames) => {
      return gameId ? allGames[itemMap[gameId]] : undefined;
    }
  );
}

function useGame(gameId: number | undefined) {
  return useSelector(createGameSelector(gameId));
}

export default useGame;
