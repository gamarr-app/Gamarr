import { createSelector, Selector } from 'reselect';

interface DiscoverGameProps {
  gameId: number;
}

interface DiscoverGame {
  igdbId: number;
  [key: string]: unknown;
}

interface DiscoverGameState {
  items: DiscoverGame[];
}

interface RootState {
  discoverGame: DiscoverGameState;
  [key: string]: unknown;
}

function createDiscoverGameSelector(): Selector<
  RootState,
  DiscoverGame | undefined,
  [DiscoverGameProps]
> {
  return createSelector(
    (_state: RootState, { gameId }: DiscoverGameProps) => gameId,
    (state: RootState) => state.discoverGame,
    (gameId, allGames) => {
      return allGames.items.find((game) => game.igdbId === gameId);
    }
  );
}

export default createDiscoverGameSelector;
