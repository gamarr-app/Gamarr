import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import DiscoverGamePosters from './DiscoverGamePosters';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.discoverGame.posterOptions,
    createUISettingsSelector(),
    createDimensionsSelector(),
    (posterOptions, uiSettings, dimensions) => {
      return {
        posterOptions,
        showRelativeDates: uiSettings.showRelativeDates,
        shortDateFormat: uiSettings.shortDateFormat,
        timeFormat: uiSettings.timeFormat,
        isSmallScreen: dimensions.isSmallScreen,
      };
    }
  );
}

export default connect(createMapStateToProps)(DiscoverGamePosters);
