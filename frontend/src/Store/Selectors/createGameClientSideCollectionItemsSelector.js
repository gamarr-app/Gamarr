import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('games', uiSection),
    (games) => {
      const items = games.items.map((s) => {
        const {
          id,
          sortTitle,
          collectionId
        } = s;

        return {
          id,
          sortTitle,
          collectionId
        };
      });

      return {
        ...games,
        items
      };
    }
  );
}

function gameListEqual(a, b) {
  return hasDifferentItemsOrOrder(a, b);
}

const createGameEqualSelector = createSelectorCreator(
  defaultMemoize,
  gameListEqual
);

function createGameClientSideCollectionItemsSelector(uiSection) {
  return createGameEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createGameClientSideCollectionItemsSelector;
