import _ from 'lodash';
import { createSelector } from 'reselect';
import AppState, {
  CustomFilter,
  Filter,
  PropertyFilter,
} from 'App/State/AppState';
import {
  filterTypePredicates,
  filterTypes,
  sortDirections,
} from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';

type FilterType = string;
type SortPredicate<T> = (item: T, sortDirection: SortDirection) => unknown;
type FilterPredicate<T> = (
  item: T,
  value: unknown,
  type: FilterType
) => boolean;

interface SortPredicates<T> {
  [key: string]: SortPredicate<T>;
}

interface FilterPredicates<T> {
  [key: string]: FilterPredicate<T>;
}

interface FilterState<T> {
  selectedFilterKey?: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  filterPredicates?: FilterPredicates<T>;
}

interface SortState<T> {
  sortKey: string;
  sortDirection: SortDirection;
  sortPredicates?: SortPredicates<T>;
  secondarySortKey?: string;
  secondarySortDirection?: SortDirection;
}

interface CollectionState<T> extends FilterState<T>, SortState<T> {
  items: T[];
}

export interface CollectionResult<T> {
  items: T[];
  totalItems: number;
  customFilters: CustomFilter[];
  [key: string]: unknown;
}

function getSortClause<T extends Record<string, unknown>>(
  sortKey: string,
  sortDirection: SortDirection,
  sortPredicates?: SortPredicates<T>
): (item: T) => unknown {
  if (sortPredicates && sortPredicates.hasOwnProperty(sortKey)) {
    return function (item: T): unknown {
      return sortPredicates[sortKey](item, sortDirection);
    };
  }

  return function (item: T): unknown {
    return item[sortKey];
  };
}

function filter<T extends Record<string, unknown>>(
  items: T[],
  state: FilterState<T>
): T[] {
  const { selectedFilterKey, filters, customFilters, filterPredicates } = state;

  if (!selectedFilterKey) {
    return items;
  }

  const selectedFilters = findSelectedFilters(
    selectedFilterKey,
    filters,
    customFilters
  );

  return _.filter(items, (item: T) => {
    let i = 0;
    let accepted = true;

    while (accepted && i < selectedFilters.length) {
      const {
        key,
        value,
        type = filterTypes.EQUAL,
      } = selectedFilters[i] as PropertyFilter;

      if (filterPredicates && filterPredicates.hasOwnProperty(key)) {
        const predicate = filterPredicates[key];

        if (Array.isArray(value)) {
          if (
            type === filterTypes.NOT_CONTAINS ||
            type === filterTypes.NOT_EQUAL
          ) {
            accepted = value.every((v) => predicate(item, v, type));
          } else {
            accepted = value.some((v) => predicate(item, v, type));
          }
        } else {
          accepted = predicate(item, value, type);
        }
      } else if (item.hasOwnProperty(key)) {
        const predicate =
          filterTypePredicates[type as keyof typeof filterTypePredicates];

        if (Array.isArray(value)) {
          if (
            type === filterTypes.NOT_CONTAINS ||
            type === filterTypes.NOT_EQUAL
          ) {
            accepted = value.every((v) => predicate(item[key], v));
          } else {
            accepted = value.some((v) => predicate(item[key], v));
          }
        } else {
          accepted = predicate(item[key], value);
        }
      } else {
        // Default to false if the filter can't be tested
        accepted = false;
      }

      i++;
    }

    return accepted;
  });
}

function sort<T extends Record<string, unknown>>(
  items: T[],
  state: SortState<T>
): T[] {
  const {
    sortKey,
    sortDirection,
    sortPredicates,
    secondarySortKey,
    secondarySortDirection,
  } = state;

  const clauses: Array<(item: T) => unknown> = [];
  const orders: Array<'asc' | 'desc'> = [];

  clauses.push(getSortClause(sortKey, sortDirection, sortPredicates));
  orders.push(sortDirection === sortDirections.ASCENDING ? 'asc' : 'desc');

  if (
    secondarySortKey &&
    secondarySortDirection &&
    (sortKey !== secondarySortKey || sortDirection !== secondarySortDirection)
  ) {
    clauses.push(
      getSortClause(secondarySortKey, secondarySortDirection, sortPredicates)
    );
    orders.push(
      secondarySortDirection === sortDirections.ASCENDING ? 'asc' : 'desc'
    );
  }

  return _.orderBy(items, clauses, orders);
}

export function createCustomFiltersSelector(
  type: string,
  alternateType?: string
) {
  return createSelector(
    (state: AppState) => state.customFilters.items,
    (customFilters) => {
      return customFilters.filter((customFilter) => {
        return (
          customFilter.type === type || customFilter.type === alternateType
        );
      });
    }
  );
}

type ItemType = Record<string, unknown>;

function createClientSideCollectionSelector(
  section: string,
  uiSection?: string
) {
  return createSelector(
    (state: AppState) => _.get(state, section) as CollectionState<ItemType>,
    (state: AppState) =>
      (uiSection ? _.get(state, uiSection) : {}) as Partial<
        CollectionState<ItemType>
      >,
    createCustomFiltersSelector(section, uiSection),
    (
      sectionState: CollectionState<ItemType>,
      uiSectionState: Partial<CollectionState<ItemType>>,
      customFilters: CustomFilter[]
    ): CollectionResult<ItemType> => {
      const state = Object.assign({}, sectionState, uiSectionState || {}, {
        customFilters,
      });

      const filtered = filter(state.items, state);
      const sorted = sort(filtered, state);

      return {
        ...sectionState,
        ...uiSectionState,
        customFilters,
        items: sorted,
        totalItems: state.items.length,
      };
    }
  );
}

export default createClientSideCollectionSelector;
