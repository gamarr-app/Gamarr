import { CustomFilter, Filter, PropertyFilter } from 'App/State/AppState';

export default function findSelectedFilters(
  selectedFilterKey: string | number | undefined,
  filters: Filter[] = [],
  customFilters: CustomFilter[] = []
): PropertyFilter[] {
  if (!selectedFilterKey) {
    return [];
  }

  let selectedFilter: Filter | CustomFilter | undefined = filters.find(
    (f) => f.key === selectedFilterKey
  );

  if (!selectedFilter) {
    selectedFilter = customFilters.find((f) => f.id === selectedFilterKey);
  }

  if (!selectedFilter) {
    if (process.env.NODE_ENV === 'development') {
      throw new Error(
        `Matching filter not found for key: ${selectedFilterKey}`
      );
    }
    return [];
  }

  return selectedFilter.filters;
}
