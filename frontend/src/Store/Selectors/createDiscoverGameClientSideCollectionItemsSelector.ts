import { createSelector, Selector } from 'reselect';
import AppState from 'App/State/AppState';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';
import createDeepEqualSelector from './createDeepEqualSelector';

interface DiscoverGameItem {
  igdbId: number;
  sortTitle: string;
}

interface DiscoverGameResult {
  items: DiscoverGameItem[];
  [key: string]: unknown;
}

interface GameWithIgdbId {
  igdbId: number;
  sortTitle: string;
  [key: string]: unknown;
}

function createUnoptimizedSelector(uiSection: string) {
  return createSelector(
    createClientSideCollectionSelector('games', uiSection),
    (games): DiscoverGameResult => {
      const items = (games.items as GameWithIgdbId[]).map((s) => {
        const { igdbId, sortTitle } = s;

        return {
          igdbId,
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

function createDiscoverGameClientSideCollectionItemsSelector(
  uiSection: string
): Selector<AppState, DiscoverGameResult> {
  return createDeepEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createDiscoverGameClientSideCollectionItemsSelector;
