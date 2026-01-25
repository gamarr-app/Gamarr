import _ from 'lodash';
import { createSelector, Selector } from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';
import gameEntities from 'Game/gameEntities';

interface GamesState {
  items: Game[];
}

interface GameByEntityProps {
  gameId: number;
  gameEntity?: string;
}

interface GameSelectorProps {
  gameId: number;
}

export function createGameSelectorForHook(
  gameId: number
): Selector<AppState, Game | undefined> {
  return createSelector(
    (state: AppState) => state.games.itemMap,
    (state: AppState) => state.games.items,
    (itemMap, allGames) => {
      return gameId ? allGames[itemMap[gameId]] : undefined;
    }
  );
}

export function createGameByEntitySelector(): Selector<
  AppState,
  Game | undefined,
  [GameByEntityProps]
> {
  return createSelector(
    (_state: AppState, { gameId }: GameByEntityProps) => gameId,
    (state: AppState, { gameEntity = gameEntities.GAMES }: GameByEntityProps) =>
      _.get(state, gameEntity, { items: [] }) as GamesState,
    (gameId, games) => {
      return _.find(games.items, { id: gameId });
    }
  );
}

function createGameSelector(): Selector<
  AppState,
  Game | undefined,
  [GameSelectorProps]
> {
  return createSelector(
    (_state: AppState, { gameId }: GameSelectorProps) => gameId,
    (state: AppState) => state.games.itemMap,
    (state: AppState) => state.games.items,
    (gameId, itemMap, allGames) => {
      return allGames[itemMap[gameId]];
    }
  );
}

export default createGameSelector;
