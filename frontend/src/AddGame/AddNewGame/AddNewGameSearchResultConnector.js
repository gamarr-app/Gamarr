import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createExistingGameSelector from 'Store/Selectors/createExistingGameSelector';
import AddNewGameSearchResult from './AddNewGameSearchResult';

function createMapStateToProps() {
  return createSelector(
    createExistingGameSelector(),
    createDimensionsSelector(),
    (state) => state.gameFiles.items,
    (state, { internalId }) => internalId,
    (state) => state.settings.ui.item.gameRuntimeFormat,
    (isExistingGame, dimensions, gameFiles, internalId, gameRuntimeFormat) => {
      const gameFile = gameFiles.find((item) => internalId > 0 && item.gameId === internalId);

      return {
        existingGameId: internalId,
        isExistingGame,
        isSmallScreen: dimensions.isSmallScreen,
        gameFile,
        gameRuntimeFormat
      };
    }
  );
}

export default connect(createMapStateToProps)(AddNewGameSearchResult);
