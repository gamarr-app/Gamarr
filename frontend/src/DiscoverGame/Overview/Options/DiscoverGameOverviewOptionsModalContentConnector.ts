import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  setListGameOption,
  setListGameOverviewOption,
} from 'Store/Actions/discoverGameActions';
import DiscoverGameOverviewOptionsModalContent from './DiscoverGameOverviewOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.discoverGame,
    (discoverGame) => {
      return {
        ...discoverGame.options,
        ...discoverGame.overviewOptions,
      };
    }
  );
}

function createMapDispatchToProps(dispatch: Dispatch) {
  return {
    onChangeOverviewOption(payload: Record<string, unknown>) {
      dispatch(setListGameOverviewOption(payload));
    },
    onChangeOption(payload: Record<string, unknown>) {
      dispatch(setListGameOption(payload));
    },
  };
}

export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(DiscoverGameOverviewOptionsModalContent);
