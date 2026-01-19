import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export interface GameQueueDetails {
  count: number;
}

function createGameQueueDetailsSelector(gameId: number) {
  return createSelector(
    (state: AppState) => state.queue.details.items,
    (queueItems) => {
      return queueItems.reduce(
        (acc: GameQueueDetails, item) => {
          if (
            item.trackedDownloadState === 'imported' ||
            item.gameId !== gameId
          ) {
            return acc;
          }

          acc.count++;

          return acc;
        },
        {
          count: 0,
        }
      );
    }
  );
}

export default createGameQueueDetailsSelector;
