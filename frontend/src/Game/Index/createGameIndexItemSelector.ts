import { createSelector } from 'reselect';
import Command from 'Commands/Command';
import { GAME_SEARCH, REFRESH_GAME } from 'Commands/commandNames';
import Game from 'Game/Game';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';
import createGameQualityProfileSelector from 'Store/Selectors/createGameQualityProfileSelector';
import { createGameSelectorForHook } from 'Store/Selectors/createGameSelector';

function createGameIndexItemSelector(gameId: number) {
  return createSelector(
    createGameSelectorForHook(gameId),
    createGameQualityProfileSelector(gameId),
    createExecutingCommandsSelector(),
    (game: Game, qualityProfile, executingCommands: Command[]) => {
      const isRefreshingGame = executingCommands.some((command) => {
        return (
          command.name === REFRESH_GAME &&
          command.body.gameIds?.includes(gameId)
        );
      });

      const isSearchingGame = executingCommands.some((command) => {
        return (
          command.name === GAME_SEARCH &&
          command.body.gameIds?.includes(gameId)
        );
      });

      return {
        game,
        qualityProfile,
        isRefreshingGame,
        isSearchingGame,
      };
    }
  );
}

export default createGameIndexItemSelector;
