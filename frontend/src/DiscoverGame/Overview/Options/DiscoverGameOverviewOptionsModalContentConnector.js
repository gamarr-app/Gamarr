import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setListGameOption, setListGameOverviewOption } from 'Store/Actions/discoverGameActions';
import DiscoverGameOverviewOptionsModalContent from './DiscoverGameOverviewOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame,
    (discoverGame) => {
      return {
        ...discoverGame.options,
        ...discoverGame.overviewOptions
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangeOverviewOption(payload) {
      dispatch(setListGameOverviewOption(payload));
    },
    onChangeOption(payload) {
      dispatch(setListGameOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DiscoverGameOverviewOptionsModalContent);
