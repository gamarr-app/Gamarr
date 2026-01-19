import createAjaxRequest from 'Utilities/createAjaxRequest';
import updateGames from 'Utilities/Game/updateGames';
import getSectionState from 'Utilities/State/getSectionState';

function createBatchToggleGameMonitoredHandler(section, fetchHandler) {
  return function(getState, payload, dispatch) {
    const {
      gameIds,
      monitored
    } = payload;

    const state = getSectionState(getState(), section, true);

    dispatch(updateGames(section, state.items, gameIds, {
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/game/editor',
      method: 'PUT',
      data: JSON.stringify({ gameIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done(() => {
      dispatch(updateGames(section, state.items, gameIds, {
        isSaving: false,
        monitored
      }));

      dispatch(fetchHandler());
    });

    promise.fail(() => {
      dispatch(updateGames(section, state.items, gameIds, {
        isSaving: false
      }));
    });
  };
}

export default createBatchToggleGameMonitoredHandler;
