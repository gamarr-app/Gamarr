import { createAction } from 'redux-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';

export const section = 'organizePreview';

export interface OrganizePreviewState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: unknown[];
}

export const defaultState: OrganizePreviewState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],
};

export const FETCH_ORGANIZE_PREVIEW = 'organizePreview/fetchOrganizePreview';
export const CLEAR_ORGANIZE_PREVIEW = 'organizePreview/clearOrganizePreview';

export const fetchOrganizePreview = createThunk(FETCH_ORGANIZE_PREVIEW);
export const clearOrganizePreview = createAction(CLEAR_ORGANIZE_PREVIEW);

export const actionHandlers = handleThunks({
  [FETCH_ORGANIZE_PREVIEW]: createFetchHandler('organizePreview', '/rename'),
});

export const reducers = createHandleActions(
  {
    [CLEAR_ORGANIZE_PREVIEW]: (state: OrganizePreviewState) => {
      return Object.assign({}, state, defaultState);
    },
  },
  defaultState,
  section
);
