import { Dispatch } from 'redux';
import pages from 'Utilities/pages';
import serverSideCollectionHandlers from 'Utilities/serverSideCollectionHandlers';
import createFetchServerSideCollectionHandler from './createFetchServerSideCollectionHandler';
import createSetServerSideCollectionFilterHandler from './createSetServerSideCollectionFilterHandler';
import createSetServerSideCollectionPageHandler from './createSetServerSideCollectionPageHandler';
import createSetServerSideCollectionSortHandler from './createSetServerSideCollectionSortHandler';

type ServerSideCollectionHandlerKey =
  (typeof serverSideCollectionHandlers)[keyof typeof serverSideCollectionHandlers];

type HandlersMap = Partial<Record<ServerSideCollectionHandlerKey, string>>;

type FetchThunk = (payload?: Record<string, unknown>) => unknown;

type FetchDataAugmenter = (
  getState: () => unknown,
  payload: Record<string, unknown>,
  data: Record<string, unknown>
) => void;

type ActionHandler = (
  getState: () => unknown,
  payload: unknown,
  dispatch: Dispatch
) => unknown;

function createServerSideCollectionHandlers(
  section: string,
  url: string,
  fetchThunk: FetchThunk,
  handlers: HandlersMap,
  fetchDataAugmenter?: FetchDataAugmenter
): Record<string, ActionHandler> {
  const actionHandlers: Record<string, ActionHandler> = {};
  const fetchHandlerType = handlers[serverSideCollectionHandlers.FETCH];
  const fetchHandler = createFetchServerSideCollectionHandler(
    section,
    url,
    fetchDataAugmenter
  ) as ActionHandler;

  if (fetchHandlerType) {
    actionHandlers[fetchHandlerType] = fetchHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.FIRST_PAGE
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.FIRST_PAGE]!;
    actionHandlers[handlerType] = createSetServerSideCollectionPageHandler(
      section,
      pages.FIRST,
      fetchThunk
    ) as ActionHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.PREVIOUS_PAGE
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.PREVIOUS_PAGE]!;
    actionHandlers[handlerType] = createSetServerSideCollectionPageHandler(
      section,
      pages.PREVIOUS,
      fetchThunk
    ) as ActionHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.NEXT_PAGE
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.NEXT_PAGE]!;
    actionHandlers[handlerType] = createSetServerSideCollectionPageHandler(
      section,
      pages.NEXT,
      fetchThunk
    ) as ActionHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.LAST_PAGE
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.LAST_PAGE]!;
    actionHandlers[handlerType] = createSetServerSideCollectionPageHandler(
      section,
      pages.LAST,
      fetchThunk
    ) as ActionHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.EXACT_PAGE
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.EXACT_PAGE]!;
    actionHandlers[handlerType] = createSetServerSideCollectionPageHandler(
      section,
      pages.EXACT,
      fetchThunk
    ) as ActionHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.SORT
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.SORT]!;
    actionHandlers[handlerType] = createSetServerSideCollectionSortHandler(
      section,
      fetchThunk
    ) as ActionHandler;
  }

  if (
    Object.prototype.hasOwnProperty.call(
      handlers,
      serverSideCollectionHandlers.FILTER
    )
  ) {
    const handlerType = handlers[serverSideCollectionHandlers.FILTER]!;
    actionHandlers[handlerType] = createSetServerSideCollectionFilterHandler(
      section,
      fetchThunk
    ) as ActionHandler;
  }

  return actionHandlers;
}

export default createServerSideCollectionHandlers;
