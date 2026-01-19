import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createAllGamesSelector() {
  return createSelector(
    (state: AppState) => state.games,
    (games) => {
      return games.items;
    }
  );
}

export default createAllGamesSelector;
