import { connect, ConnectedProps } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { AddGameState } from 'Store/Actions/addGameActions';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import { AppDispatch } from 'Store/thunks';
import ImportGameTable from './ImportGameTable';

interface AddGameAppState {
  addGame: AddGameState;
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState & AddGameAppState) => state.addGame,
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

function createMapDispatchToProps(dispatch: AppDispatch) {
  return {
    onGameLookup(name: string, path: string, relativePath: string) {
      dispatch(
        queueLookupGame({
          name,
          path,
          relativePath,
          term: name,
        })
      );
    },

    onSetImportGameValue(values: { id: string; [key: string]: unknown }) {
      dispatch(setImportGameValue(values));
    },
  };
}

const connector = connect(createMapStateToProps, createMapDispatchToProps);

export type ImportGameTableConnectorProps = ConnectedProps<typeof connector>;

export default connector(ImportGameTable);
