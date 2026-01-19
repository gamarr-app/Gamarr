import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { queueLookupGame, setImportGameValue } from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import ImportGameTable from './ImportGameTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.addGame,
    (state) => state.importGame,
    (state) => state.app.dimensions,
    createAllGamesSelector(),
    (addGame, importGame, dimensions, allGames) => {
      return {
        defaultMonitor: addGame.defaults.monitor,
        defaultQualityProfileId: addGame.defaults.qualityProfileId,
        defaultMinimumAvailability: addGame.defaults.minimumAvailability,
        items: importGame.items,
        isSmallScreen: dimensions.isSmallScreen,
        allGames
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGameLookup(name, path, relativePath) {
      dispatch(queueLookupGame({
        name,
        path,
        relativePath,
        term: name
      }));
    },

    onSetImportGameValue(values) {
      dispatch(setImportGameValue(values));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(ImportGameTable);
