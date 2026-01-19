import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllGamesSelector from './createAllGamesSelector';

function createGameCountSelector() {
  return createSelector(
    createAllGamesSelector(),
    (state: AppState) => state.games.error,
    (state: AppState) => state.games.isFetching,
    (state: AppState) => state.games.isPopulated,
    (games, error, isFetching, isPopulated) => {
      return {
        count: games.length,
        error,
        isFetching,
        isPopulated,
      };
    }
  );
}

export default createGameCountSelector;
