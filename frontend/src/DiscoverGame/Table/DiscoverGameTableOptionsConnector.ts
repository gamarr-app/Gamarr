import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { setListGameOption } from 'Store/Actions/discoverGameActions';
import DiscoverGameTableOptions from './DiscoverGameTableOptions';

interface DiscoverGameOptions {
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
}

interface PartialDiscoverGameOptions {
  includeRecommendations?: boolean;
  includeTrending?: boolean;
  includePopular?: boolean;
}

interface DiscoverGameState {
  options: DiscoverGameOptions;
}

type ExtendedAppState = AppState & {
  discoverGame: DiscoverGameState;
};

function createMapStateToProps() {
  return createSelector(
    (state: ExtendedAppState) => state.discoverGame,
    (discoverGame) => {
      return discoverGame.options;
    }
  );
}

function createMapDispatchToProps(dispatch: Dispatch) {
  return {
    onChangeOption(payload: PartialDiscoverGameOptions) {
      dispatch(setListGameOption(payload));
    },
  };
}

export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(DiscoverGameTableOptions);
