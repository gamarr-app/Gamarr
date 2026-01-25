import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import pages from 'Utilities/pages';
import getSectionState from 'Utilities/State/getSectionState';

type PageType = (typeof pages)[keyof typeof pages];

interface SectionState {
  page?: number;
  totalPages?: number;
  [key: string]: unknown;
}

interface PagePayload {
  page?: number;
}

type GetState = () => AppState;
type FetchHandler = (payload?: { page: number }) => unknown;

function createSetServerSideCollectionPageHandler(
  section: string,
  page: PageType,
  fetchHandler: FetchHandler
) {
  return function (
    getState: GetState,
    payload: PagePayload,
    dispatch: Dispatch
  ): void {
    const sectionState = getSectionState(
      getState() as unknown as Record<string, unknown>,
      section,
      true
    ) as SectionState;
    const currentPage = sectionState.page || 1;
    let nextPage = 0;

    switch (page) {
      case pages.FIRST:
        nextPage = 1;
        break;
      case pages.PREVIOUS:
        nextPage = currentPage - 1;
        break;
      case pages.NEXT:
        nextPage = currentPage + 1;
        break;
      case pages.LAST:
        nextPage = sectionState.totalPages || 1;
        break;
      default:
        nextPage = payload.page || 1;
    }

    dispatch(fetchHandler({ page: nextPage }) as never);
  };
}

export default createSetServerSideCollectionPageHandler;
