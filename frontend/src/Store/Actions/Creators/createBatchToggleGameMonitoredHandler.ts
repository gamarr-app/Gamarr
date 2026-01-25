import { Dispatch } from 'redux';
import AppState from 'App/State/AppState';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import updateGames from 'Utilities/Game/updateGames';
import getSectionState from 'Utilities/State/getSectionState';

interface BatchTogglePayload {
  gameIds: number[];
  monitored: boolean;
}

interface SectionState {
  items: unknown[];
  [key: string]: unknown;
}

type GetState = () => AppState;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type FetchHandler = () => any;

function createBatchToggleGameMonitoredHandler(
  section: string,
  fetchHandler: FetchHandler
) {
  return function (
    getState: GetState,
    payload: BatchTogglePayload,
    dispatch: Dispatch
  ): void {
    const { gameIds, monitored } = payload;

    const state = getSectionState(
      getState() as unknown as Record<string, unknown>,
      section,
      true
    ) as SectionState;

    dispatch(
      updateGames(section, state.items as never[], gameIds, {
        isSaving: true,
      })
    );

    const promise = createAjaxRequest({
      url: '/game/editor',
      method: 'PUT',
      data: JSON.stringify({ gameIds, monitored }),
      dataType: 'json',
    }).request;

    promise.done(() => {
      dispatch(
        updateGames(section, state.items as never[], gameIds, {
          isSaving: false,
          monitored,
        })
      );

      dispatch(fetchHandler());
    });

    promise.fail(() => {
      dispatch(
        updateGames(section, state.items as never[], gameIds, {
          isSaving: false,
        })
      );
    });
  };
}

export default createBatchToggleGameMonitoredHandler;
