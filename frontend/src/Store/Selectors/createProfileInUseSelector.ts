import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';
import GameCollection from 'typings/GameCollection';
import ImportList from 'typings/ImportList';
import createAllGamesSelector from './createAllGamesSelector';

function createProfileInUseSelector(profileProp: string) {
  return createSelector(
    (_: AppState, { id }: { id: number }) => id,
    createAllGamesSelector(),
    (state: AppState) => state.settings.importLists.items,
    (state: AppState) => state.gameCollections.items,
    (id, games, lists, collections) => {
      if (!id) {
        return false;
      }

      return (
        games.some((m) => m[profileProp as keyof Game] === id) ||
        lists.some((list) => list[profileProp as keyof ImportList] === id) ||
        collections.some(
          (collection) => collection[profileProp as keyof GameCollection] === id
        )
      );
    }
  );
}

export default createProfileInUseSelector;
