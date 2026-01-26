import _ from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Game, { GameMonitor } from 'Game/Game';
import createAllGamesSelector from './createAllGamesSelector';

interface ImportGameItemProps {
  id: string;
}

interface SelectedGame {
  igdbId: number;
  [key: string]: unknown;
}

interface ImportGameItem {
  id: string;
  selectedGame?: SelectedGame;
  [key: string]: unknown;
}

interface ImportGameItemResult {
  defaultMonitor: GameMonitor;
  defaultQualityProfileId: number;
  isExistingGame: boolean;
  [key: string]: unknown;
}

function createImportGameItemSelector() {
  return createSelector(
    (_state: AppState, { id }: ImportGameItemProps) => id,
    (state: AppState) => state.addGame,
    (state: AppState) => state.importGame,
    createAllGamesSelector(),
    (id, addGame, importGame, games: Game[]): ImportGameItemResult => {
      const item = _.find(importGame.items, { id }) || {};
      const selectedGame = (item as ImportGameItem).selectedGame;
      const isExistingGame =
        !!selectedGame && _.some(games, { igdbId: selectedGame.igdbId });

      return {
        defaultMonitor: addGame.defaults.monitor,
        defaultQualityProfileId: addGame.defaults.qualityProfileId,
        ...item,
        isExistingGame,
      };
    }
  );
}

export default createImportGameItemSelector;
