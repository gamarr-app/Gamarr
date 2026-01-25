import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { update } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';

export const section = 'tags';

interface Tag {
  id: number;
  label: string;
}

interface TagDetailsState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: unknown[];
}

export interface TagsState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: Tag[];
  details: TagDetailsState;
}

interface AddTagPayload {
  tag: Tag;
  onTagCreated: (tag: Tag) => void;
}

export const defaultState: TagsState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],

  details: {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: [],
  },
};

export const FETCH_TAGS = 'tags/fetchTags';
export const ADD_TAG = 'tags/addTag';
export const DELETE_TAG = 'tags/deleteTag';
export const FETCH_TAG_DETAILS = 'tags/fetchTagDetails';

export const fetchTags = createThunk(FETCH_TAGS);
export const addTag = createThunk(ADD_TAG);
export const deleteTag = createThunk(DELETE_TAG);
export const fetchTagDetails = createThunk(FETCH_TAG_DETAILS);

export const actionHandlers = handleThunks({
  [FETCH_TAGS]: createFetchHandler(section, '/tag'),

  [ADD_TAG]: function (
    getState: () => AppState,
    payload: AddTagPayload,
    dispatch: Dispatch
  ) {
    const promise = createAjaxRequest({
      url: '/tag',
      method: 'POST',
      data: JSON.stringify(payload.tag),
      dataType: 'json',
    }).request;

    promise.done((data: Tag) => {
      const tags = getState().tags.items.slice();
      tags.push(data);

      dispatch(update({ section, data: tags }));
      payload.onTagCreated(data);
    });
  },

  [DELETE_TAG]: createRemoveItemHandler(section, '/tag'),
  [FETCH_TAG_DETAILS]: createFetchHandler('tags.details', '/tag/detail'),
});

export const reducers = createHandleActions({}, defaultState, section);
