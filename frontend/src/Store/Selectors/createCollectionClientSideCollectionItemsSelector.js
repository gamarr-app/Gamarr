import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('gameCollections', uiSection),
    (games) => {
      const items = games.items.map((s) => {
        const {
          id,
          sortTitle
        } = s;

        return {
          id,
          sortTitle
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

function createCollectionClientSideCollectionItemsSelector(uiSection) {
  return createGameEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createCollectionClientSideCollectionItemsSelector;
