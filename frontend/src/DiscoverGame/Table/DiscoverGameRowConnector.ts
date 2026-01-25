import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import DiscoverGameRow from './DiscoverGameRow';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.settings.ui.item.gameRuntimeFormat,
    createDimensionsSelector(),
    (gameRuntimeFormat, dimensions) => {
      return {
        gameRuntimeFormat,
        isSmallScreen: dimensions.isSmallScreen,
      };
    }
  );
}

export default connect(createMapStateToProps)(DiscoverGameRow);
