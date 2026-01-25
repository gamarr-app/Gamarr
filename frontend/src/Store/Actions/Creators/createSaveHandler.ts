import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getSectionState from 'Utilities/State/getSectionState';
import { set, update } from '../baseActions';

interface SectionState {
  item?: Record<string, unknown>;
  pendingChanges?: Record<string, unknown>;
  [key: string]: unknown;
}

type GetState = () => AppState;

function createSaveHandler(section: string, url: string) {
  return function (
    getState: GetState,
    payload: Record<string, unknown>,
    dispatch: Dispatch
  ): void {
    dispatch(set({ section, isSaving: true }));

    const state = getSectionState(
      getState() as unknown as Record<string, unknown>,
      section,
      true
    ) as SectionState;
    const saveData = Object.assign(
      {},
      state.item,
      state.pendingChanges,
      payload
    );

    const promise = createAjaxRequest({
      url,
      method: 'PUT',
      dataType: 'json',
      data: JSON.stringify(saveData),
    }).request;

    promise.done((data: unknown) => {
      dispatch(
        batchActions([
          update({ section, data }),

          set({
            section,
            isSaving: false,
            saveError: null,
            pendingChanges: {},
          }),
        ])
      );
    });

    promise.fail((xhr) => {
      dispatch(
        set({
          section,
          isSaving: false,
          saveError: xhr,
        })
      );
    });
  };
}

export default createSaveHandler;
