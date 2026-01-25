import _ from 'lodash';
import { createSelector } from 'reselect';
import Game, { GameMonitor } from 'Game/Game';
import createAllGamesSelector from './createAllGamesSelector';

interface ImportGameItemProps {
  id: number;
}

interface SelectedGame {
  igdbId: number;
  [key: string]: unknown;
}

interface ImportGameItem {
  id: number;
  selectedGame?: SelectedGame;
  [key: string]: unknown;
}

interface AddGameState {
  defaults: {
    monitor: GameMonitor;
    qualityProfileId: number;
    [key: string]: unknown;
  };
}

interface ImportGameState {
  items: ImportGameItem[];
}

interface ImportGameItemResult {
  defaultMonitor: GameMonitor;
  defaultQualityProfileId: number;
  isExistingGame: boolean;
  [key: string]: unknown;
}

function createImportGameItemSelector() {
  return createSelector(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (_state: any, { id }: ImportGameItemProps) => id,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.addGame as AddGameState,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.importGame as ImportGameState,
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
