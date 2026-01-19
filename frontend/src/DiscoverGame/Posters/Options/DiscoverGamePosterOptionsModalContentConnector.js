import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setListGameOption, setListGamePosterOption } from 'Store/Actions/discoverGameActions';
import DiscoverGamePosterOptionsModalContent from './DiscoverGamePosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame,
    (discoverGame) => {
      return {
        ...discoverGame.options,
        ...discoverGame.posterOptions
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangePosterOption(payload) {
      dispatch(setListGamePosterOption(payload));
    },
    onChangeOption(payload) {
      dispatch(setListGameOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DiscoverGamePosterOptionsModalContent);
