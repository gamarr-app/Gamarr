import { createAction } from 'redux-actions';

//
// Action Types

export const SET = 'base/set';

export const UPDATE = 'base/update';
export const UPDATE_ITEM = 'base/updateItem';
export const UPDATE_SERVER_SIDE_COLLECTION = 'base/updateServerSideCollection';

export const SET_SETTING_VALUE = 'base/setSettingValue';
export const CLEAR_PENDING_CHANGES = 'base/clearPendingChanges';

export const REMOVE_ITEM = 'base/removeItem';

//
// Action Creators

export interface SetPayload {
  section: string;
  [key: string]: unknown;
}

export interface UpdatePayload {
  section: string;
  data: unknown[] | unknown;
}

export interface UpdateItemPayload {
  section: string;
  id?: number | string;
  updateOnly?: boolean;
  [key: string]: unknown;
}

export interface SetSettingValuePayload {
  section: string;
  name: string;
  value: unknown;
}

export interface ClearPendingChangesPayload {
  section: string;
}

export interface RemoveItemPayload {
  section: string;
  id: number | string;
}

export const set = createAction<SetPayload>(SET);

export const update = createAction<UpdatePayload>(UPDATE);
export const updateItem = createAction<UpdateItemPayload>(UPDATE_ITEM);
export const updateServerSideCollection = createAction<UpdatePayload>(
  UPDATE_SERVER_SIDE_COLLECTION
);

export const setSettingValue =
  createAction<SetSettingValuePayload>(SET_SETTING_VALUE);
export const clearPendingChanges = createAction<ClearPendingChangesPayload>(
  CLEAR_PENDING_CHANGES
);

export const removeItem = createAction<RemoveItemPayload>(REMOVE_ITEM);
