import { some } from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllGamesSelector from './createAllGamesSelector';

function createExistingGameSelector() {
  return createSelector(
    (_: AppState, { igdbId }: { igdbId: number }) => igdbId,
    createAllGamesSelector(),
    (igdbId, games) => {
      return some(games, { igdbId });
    }
  );
}

export default createExistingGameSelector;
