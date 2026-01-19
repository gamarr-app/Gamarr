import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import DiscoverGamePoster from './DiscoverGamePoster';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.ui.item.gameRuntimeFormat,
    createDimensionsSelector(),
    (gameRuntimeFormat, dimensions) => {
      return {
        gameRuntimeFormat,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

export default connect(createMapStateToProps)(DiscoverGamePoster);
