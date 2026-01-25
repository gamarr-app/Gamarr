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
    // TODO: throw in dev
    console.error('Matching filter not found');
    return [];
  }

  return selectedFilter.filters;
}
