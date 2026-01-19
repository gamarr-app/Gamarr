import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createQueueStatusSelector() {
  return createSelector(
    (state: AppState) => state.queue.status.isPopulated,
    (state: AppState) => state.queue.status.item,
    (state: AppState) => state.queue.options.includeUnknownGameItems,
    (isPopulated, status, includeUnknownGameItems) => {
      const {
        errors,
        warnings,
        unknownErrors,
        unknownWarnings,
        count,
        totalCount,
      } = status;

      return {
        ...status,
        isPopulated,
        count: includeUnknownGameItems ? totalCount : count,
        errors: includeUnknownGameItems ? errors || unknownErrors : errors,
        warnings: includeUnknownGameItems
          ? warnings || unknownWarnings
          : warnings,
      };
    }
  );
}

export default createQueueStatusSelector;
