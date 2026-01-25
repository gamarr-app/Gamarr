import _ from 'lodash';
import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';
import getSectionState from 'Utilities/State/getSectionState';
import { set, updateServerSideCollection } from '../baseActions';

interface SectionState {
  page?: number;
  pageSize?: number;
  sortDirection?: string;
  sortKey?: string;
  selectedFilterKey?: string;
  filters?: Filter[];
  [key: string]: unknown;
}

interface FetchPayload {
  page?: number;
  [key: string]: unknown;
}

interface FetchData {
  page: number;
  pageSize?: number;
  sortDirection?: string;
  sortKey?: string;
  [key: string]: unknown;
}

type GetState = () => AppState;
type FetchDataAugmenter = (
  getState: GetState,
  payload: FetchPayload,
  data: FetchData
) => void;

function createFetchServerSideCollectionHandler(
  section: string,
  url: string,
  fetchDataAugmenter?: FetchDataAugmenter
) {
  const [baseSection] = section.split('.');

  return function (
    getState: GetState,
    payload: FetchPayload,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, isFetching: true }));

    const sectionState = getSectionState(
      getState() as unknown as Record<string, unknown>,
      section,
      true
    ) as SectionState;
    const page = payload.page || sectionState.page || 1;

    const data: FetchData = Object.assign(
      { page },
      _.pick(sectionState, ['pageSize', 'sortDirection', 'sortKey'])
    );

    if (fetchDataAugmenter) {
      fetchDataAugmenter(getState, payload, data);
    }

    const { selectedFilterKey, filters } = sectionState;

    const customFilters = (
      getState().customFilters.items as CustomFilter[]
    ).filter((customFilter) => {
      return customFilter.type === section || customFilter.type === baseSection;
    });

    const selectedFilters = findSelectedFilters(
      selectedFilterKey,
      filters,
      customFilters
    );

    selectedFilters.forEach((filter) => {
      data[filter.key] = filter.value;
    });

    const promise = createAjaxRequest({
      url,
      data,
      traditional: true,
    }).request;

    promise.done((response: unknown) => {
      dispatch(
        batchActions([
          updateServerSideCollection({ section, data: response }),

          set({
            section,
            isFetching: false,
            isPopulated: true,
            error: null,
          }),
        ])
      );
    });

    promise.fail((xhr: XMLHttpRequest) => {
      dispatch(
        set({
          section,
          isFetching: false,
          isPopulated: false,
          error: xhr,
        })
      );
    });
  };
}

export default createFetchServerSideCollectionHandler;
