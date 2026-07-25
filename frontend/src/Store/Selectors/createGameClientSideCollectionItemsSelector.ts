import {
  createSelector,
  createSelectorCreator,
  lruMemoize,
  Selector,
} from 'reselect';
import { Error } from 'App/State/AppSectionState';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import Column from 'Components/Table/Column';
import Game from 'Game/Game';
import { SortDirection } from 'Helpers/Props/sortDirections';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

export interface GameIndexItem {
  id: number;
  sortTitle: string;
  collectionId?: number;
  // Stable identity fields for platform-entry grouping (#150). Volatile
  // fields (hasFile, monitored) must NOT be added here: the collection
  // memo compares by id and order only, so changes would serve stale data.
  steamAppId?: number;
  igdbId?: number;
  year?: number;
  platform?: string;
}

export interface GameClientSideCollectionItemsState {
  isFetching: boolean;
  isPopulated: boolean;
  error: Error;
  items: GameIndexItem[];
  sortKey: string;
  sortDirection: SortDirection;
  selectedFilterKey: string;
  filters: Filter[];
  customFilters: CustomFilter[];
  totalItems: number;
  view: string;
  columns: Column[];
}

function createUnoptimizedSelector(uiSection: string) {
  return createSelector(
    createClientSideCollectionSelector<Game>('games', uiSection),
    (games): GameClientSideCollectionItemsState => {
      const items = games.items.map((s) => {
        const {
          id,
          sortTitle,
          collection,
          steamAppId,
          igdbId,
          year,
          platform,
        } = s;

        return {
          id,
          sortTitle,
          collectionId: collection?.igdbId,
          steamAppId,
          igdbId,
          year,
          platform,
        };
      });

      return {
        ...games,
        items,
      } as GameClientSideCollectionItemsState;
    }
  );
}

function gameListEqual(
  a: GameClientSideCollectionItemsState,
  b: GameClientSideCollectionItemsState
): boolean {
  // Equality check: true means "same inputs, reuse the cached result".
  // Returning hasDifferentItemsOrOrder un-negated served STALE results
  // exactly when the list changed (add/delete/sort/filter) — the frozen
  // Game Index.
  return !hasDifferentItemsOrOrder(a.items, b.items);
}

const createGameEqualSelector = createSelectorCreator(
  lruMemoize,
  gameListEqual
);

function createGameClientSideCollectionItemsSelector(
  uiSection: string
): Selector<AppState, GameClientSideCollectionItemsState> {
  return createGameEqualSelector(
    createUnoptimizedSelector(uiSection),
    (games) => games
  );
}

export default createGameClientSideCollectionItemsSelector;
