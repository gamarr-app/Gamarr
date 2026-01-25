import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Column from 'Components/Table/Column';
import { setListGameSort } from 'Store/Actions/discoverGameActions';
import DiscoverGameTable from './DiscoverGameTable';

interface DiscoverGameState {
  columns: Column[];
}

type ExtendedAppState = AppState & {
  discoverGame: DiscoverGameState;
};

function createMapStateToProps() {
  return createSelector(
    (state: ExtendedAppState) => state.app.dimensions,
    (state: ExtendedAppState) => state.discoverGame.columns,
    (dimensions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        columns,
      };
    }
  );
}

function createMapDispatchToProps(dispatch: Dispatch) {
  return {
    onSortPress(sortKey: string) {
      dispatch(setListGameSort({ sortKey }));
    },
  };
}

export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(DiscoverGameTable);
