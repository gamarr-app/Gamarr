import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import CollectionOverviews from './CollectionOverviews';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.gameCollections.overviewOptions,
    createUISettingsSelector(),
    createDimensionsSelector(),
    (overviewOptions, uiSettings, dimensions) => {
      return {
        overviewOptions: overviewOptions ?? {
          detailedProgressBar: false,
          size: 'medium',
          showDetails: true,
          showOverview: true,
          showPosters: true,
        },
        showRelativeDates: uiSettings?.showRelativeDates ?? true,
        shortDateFormat: uiSettings?.shortDateFormat ?? 'MMM D YYYY',
        longDateFormat: uiSettings?.longDateFormat ?? 'dddd, MMMM D YYYY',
        timeFormat: uiSettings?.timeFormat ?? 'h:mm a',
        isSmallScreen: dimensions.isSmallScreen,
      };
    }
  );
}

export default connect(createMapStateToProps)(CollectionOverviews);
