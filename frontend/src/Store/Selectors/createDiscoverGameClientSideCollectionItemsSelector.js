import { createSelector } from 'reselect';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';
import createDeepEqualSelector from './createDeepEqualSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('games', uiSection),
    (games) => {
      const items = games.items.map((s) => {
        const {
          igdbId,
          sortTitle
        } = s;

        return {
          igdbId,
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

function createDiscoverGameClientSideCollectionItemsSelector(uiSection) {
  return createDeepEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createDiscoverGameClientSideCollectionItemsSelector;
