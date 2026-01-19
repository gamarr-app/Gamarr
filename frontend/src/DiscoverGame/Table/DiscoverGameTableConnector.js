import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setListGameSort } from 'Store/Actions/discoverGameActions';
import DiscoverGameTable from './DiscoverGameTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.discoverGame.columns,
    (dimensions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        columns
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setListGameSort({ sortKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DiscoverGameTable);
