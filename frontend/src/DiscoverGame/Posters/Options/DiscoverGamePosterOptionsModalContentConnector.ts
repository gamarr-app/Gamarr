import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  setListGameOption,
  setListGamePosterOption,
} from 'Store/Actions/discoverGameActions';
import DiscoverGamePosterOptionsModalContent from './DiscoverGamePosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.discoverGame,
    (discoverGame) => {
      return {
        ...discoverGame.options,
        ...discoverGame.posterOptions,
      };
    }
  );
}

function createMapDispatchToProps(dispatch: Dispatch) {
  return {
    onChangePosterOption(payload: Record<string, unknown>) {
      dispatch(setListGamePosterOption(payload));
    },
    onChangeOption(payload: Record<string, unknown>) {
      dispatch(setListGameOption(payload));
    },
  };
}

export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(DiscoverGamePosterOptionsModalContent);
