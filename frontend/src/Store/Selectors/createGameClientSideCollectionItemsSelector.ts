import {
  createSelector,
  createSelectorCreator,
  defaultMemoize,
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
    createClientSideCollectionSelector('games', uiSection),
    (games): GameClientSideCollectionItemsState => {
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
      } as GameClientSideCollectionItemsState;
    }
  );
}

function gameListEqual(
  a: GameClientSideCollectionItemsState,
  b: GameClientSideCollectionItemsState
): boolean {
  return hasDifferentItemsOrOrder(a.items, b.items);
}

const createGameEqualSelector = createSelectorCreator(
  defaultMemoize,
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
