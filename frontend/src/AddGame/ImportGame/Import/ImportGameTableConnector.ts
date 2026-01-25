import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import ImportGameTable from './ImportGameTable';

function createMapStateToProps() {
  return createSelector(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.addGame,
    (state: AppState) => state.importGame,
    (state: AppState) => state.app.dimensions,
    createAllGamesSelector(),
    (addGame, importGame, dimensions, allGames) => {
      return {
        defaultMonitor: addGame.defaults.monitor,
        defaultQualityProfileId: addGame.defaults.qualityProfileId,
        defaultMinimumAvailability: addGame.defaults.minimumAvailability,
        items: importGame.items,
        isSmallScreen: dimensions.isSmallScreen,
        allGames,
      };
    }
  );
}

function createMapDispatchToProps(dispatch: Dispatch) {
  return {
    onGameLookup(name: string, path: string, relativePath: string) {
      dispatch(
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        queueLookupGame({
          name,
          path,
          relativePath,
          term: name,
        }) as any
      );
    },

    onSetImportGameValue(values: Record<string, unknown>) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      dispatch(setImportGameValue(values as any));
    },
  };
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(ImportGameTable as any);
