import { createSelector, Selector } from 'reselect';
import AppState from 'App/State/AppState';
import Command from 'Commands/Command';
import { GAME_SEARCH, REFRESH_GAME } from 'Commands/commandNames';
import Game from 'Game/Game';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';
import createGameQualityProfileSelector from 'Store/Selectors/createGameQualityProfileSelector';
import { createGameSelectorForHook } from 'Store/Selectors/createGameSelector';
import QualityProfile from 'typings/QualityProfile';

export interface GameIndexItemSelectorResult {
  game: Game;
  qualityProfile: QualityProfile | undefined;
  isRefreshingGame: boolean;
  isSearchingGame: boolean;
}

function createGameIndexItemSelector(
  gameId: number
): Selector<AppState, GameIndexItemSelectorResult> {
  return createSelector(
    createGameSelectorForHook(gameId),
    createGameQualityProfileSelector(gameId),
    createExecutingCommandsSelector(),
    (game: Game | undefined, qualityProfile, executingCommands: Command[]) => {
      const isRefreshingGame = executingCommands.some((command) => {
        return (
          command.name === REFRESH_GAME &&
          command.body.gameIds?.includes(gameId)
        );
      });

      const isSearchingGame = executingCommands.some((command) => {
        return (
          command.name === GAME_SEARCH && command.body.gameIds?.includes(gameId)
        );
      });

      return {
        game: game as Game,
        qualityProfile,
        isRefreshingGame,
        isSearchingGame,
      };
    }
  );
}

export default createGameIndexItemSelector;
