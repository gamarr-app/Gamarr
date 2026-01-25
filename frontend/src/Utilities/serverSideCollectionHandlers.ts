const serverSideCollectionHandlers = {
  FETCH: 'fetch',
  FIRST_PAGE: 'firstPage',
  PREVIOUS_PAGE: 'previousPage',
  NEXT_PAGE: 'nextPage',
  LAST_PAGE: 'lastPage',
  EXACT_PAGE: 'exactPage',
  SORT: 'sort',
  FILTER: 'filter',
} as const;

export default serverSideCollectionHandlers;
