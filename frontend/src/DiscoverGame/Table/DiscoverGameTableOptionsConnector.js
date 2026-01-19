import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setListGameOption } from 'Store/Actions/discoverGameActions';
import DiscoverGameTableOptions from './DiscoverGameTableOptions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame,
    (discoverGame) => {
      return discoverGame.options;
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangeOption(payload) {
      dispatch(setListGameOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DiscoverGameTableOptions);

