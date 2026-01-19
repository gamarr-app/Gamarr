import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createGamesFetchingSelector() {
  return createSelector(
    (state: AppState) => state.games,
    (games) => {
      return {
        isGamesFetching: games.isFetching,
        isGamesPopulated: games.isPopulated,
        gamesError: games.error,
      };
    }
  );
}

export default createGamesFetchingSelector;
