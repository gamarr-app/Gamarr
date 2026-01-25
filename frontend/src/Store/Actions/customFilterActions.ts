import { createThunk, handleThunks } from 'Store/thunks';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';

//
// Variables

export const section = 'customFilters';

//
// State

export interface CustomFiltersState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isSaving: boolean;
  saveError: unknown;
  isDeleting: boolean;
  deleteError: unknown;
  items: CustomFilterItem[];
  pendingChanges: Record<string, unknown>;
}

export interface CustomFilterItem {
  id: number;
  type: string;
  label: string;
  filters: unknown[];
}

export const defaultState: CustomFiltersState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  items: [],
  pendingChanges: {},
};

//
// Actions Types

export const FETCH_CUSTOM_FILTERS = 'customFilters/fetchCustomFilters';
export const SAVE_CUSTOM_FILTER = 'customFilters/saveCustomFilter';
export const DELETE_CUSTOM_FILTER = 'customFilters/deleteCustomFilter';

//
// Action Creators

export const fetchCustomFilters = createThunk(FETCH_CUSTOM_FILTERS);
export const saveCustomFilter = createThunk(SAVE_CUSTOM_FILTER);
export const deleteCustomFilter = createThunk(DELETE_CUSTOM_FILTER);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_CUSTOM_FILTERS]: createFetchHandler(section, '/customFilter'),

  [SAVE_CUSTOM_FILTER]: createSaveProviderHandler(section, '/customFilter'),

  [DELETE_CUSTOM_FILTER]: createRemoveItemHandler(section, '/customFilter'),
});

//
// Reducers
export const reducers = createHandleActions({}, defaultState, section);
