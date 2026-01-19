import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

const selectOverviewOptions = createSelector(
  (state: AppState) => state.gameIndex.overviewOptions,
  (overviewOptions) => overviewOptions
);

export default selectOverviewOptions;
