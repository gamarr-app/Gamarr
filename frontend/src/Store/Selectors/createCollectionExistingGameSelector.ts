import { createSelector, Selector } from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';
import createAllGamesSelector from './createAllGamesSelector';

interface CollectionExistingGameProps {
  igdbId: number;
}

function createCollectionExistingGameSelector(): Selector<
  AppState,
  Game | undefined,
  [CollectionExistingGameProps]
> {
  return createSelector(
    (_state: AppState, { igdbId }: CollectionExistingGameProps) => igdbId,
    createAllGamesSelector(),
    (igdbId, allGames) => {
      return allGames.find((game) => game.igdbId === igdbId);
    }
  );
}

export default createCollectionExistingGameSelector;
