import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export function createQueueItemSelectorForHook(gameId: number) {
  return createSelector(
    (state: AppState) => state.queue.details.items,
    (details) => {
      if (!gameId || !details) {
        return null;
      }

      return details.find((item) => item.gameId === gameId);
    }
  );
}

function createQueueItemSelector() {
  return createSelector(
    (_: AppState, { gameId }: { gameId: number }) => gameId,
    (state: AppState) => state.queue.details.items,
    (gameId, details) => {
      if (!gameId || !details) {
        return null;
      }

      return details.find((item) => item.gameId === gameId);
    }
  );
}

export default createQueueItemSelector;
