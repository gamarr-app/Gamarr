import {
  createSelector,
  createSelectorCreator,
  defaultMemoize,
  Selector,
} from 'reselect';
import AppState from 'App/State/AppState';
import GameCollection from 'typings/GameCollection';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

interface CollectionItem {
  id: number;
  sortTitle: string;
  [key: string]: unknown;
}

interface CollectionResult {
  items: CollectionItem[];
  [key: string]: unknown;
}

function createUnoptimizedSelector(uiSection: string) {
  return createSelector(
    createClientSideCollectionSelector('gameCollections', uiSection),
    (games): CollectionResult => {
      const items = (games.items as unknown as GameCollection[]).map((s) => {
        const { id, sortTitle } = s;

        return {
          id,
          sortTitle,
        };
      });

      return {
        ...games,
        items,
      };
    }
  );
}

function gameListEqual(a: CollectionResult, b: CollectionResult): boolean {
  return hasDifferentItemsOrOrder(a.items, b.items);
}

const createGameEqualSelector = createSelectorCreator(
  defaultMemoize,
  gameListEqual
);

function createCollectionClientSideCollectionItemsSelector(
  uiSection: string
): Selector<AppState, CollectionResult> {
  return createGameEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createCollectionClientSideCollectionItemsSelector;
