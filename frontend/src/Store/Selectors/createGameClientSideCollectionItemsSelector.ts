import {
  createSelector,
  createSelectorCreator,
  defaultMemoize,
  Selector,
} from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

interface GameItem {
  id: number;
  sortTitle: string;
  collectionId?: number;
  [key: string]: unknown;
}

interface GameResult {
  items: GameItem[];
  [key: string]: unknown;
}

function createUnoptimizedSelector(uiSection: string) {
  return createSelector(
    createClientSideCollectionSelector('games', uiSection),
    (games): GameResult => {
      const items = (games.items as unknown as Game[]).map((s) => {
        const { id, sortTitle, collection } = s;

        return {
          id,
          sortTitle,
          collectionId: collection?.igdbId,
        };
      });

      return {
        ...games,
        items,
      };
    }
  );
}

function gameListEqual(a: GameResult, b: GameResult): boolean {
  return hasDifferentItemsOrOrder(a.items, b.items);
}

const createGameEqualSelector = createSelectorCreator(
  defaultMemoize,
  gameListEqual
);

function createGameClientSideCollectionItemsSelector(
  uiSection: string
): Selector<AppState, GameResult> {
  return createGameEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createGameClientSideCollectionItemsSelector;
