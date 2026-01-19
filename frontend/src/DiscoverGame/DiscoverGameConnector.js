import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { executeCommand } from 'Store/Actions/commandActions';
import { addImportListExclusions, addGames, clearAddGame, fetchDiscoverGames, setListGameFilter, setListGameSort, setListGameTableOption, setListGameView } from 'Store/Actions/discoverGameActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import scrollPositions from 'Store/scrollPositions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createDiscoverGameClientSideCollectionItemsSelector from 'Store/Selectors/createDiscoverGameClientSideCollectionItemsSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import DiscoverGame from './DiscoverGame';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame,
    createDiscoverGameClientSideCollectionItemsSelector('discoverGame'),
    createCommandExecutingSelector(commandNames.IMPORT_LIST_SYNC),
    createDimensionsSelector(),
    (
      discoverGame,
      games,
      isSyncingLists,
      dimensionsState
    ) => {
      return {
        ...discoverGame.options,
        ...games,
        isSyncingLists,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    dispatchFetchRootFolders() {
      dispatch(fetchRootFolders());
    },

    dispatchClearListGame() {
      dispatch(clearAddGame());
    },

    dispatchFetchListGames() {
      dispatch(fetchDiscoverGames());
    },

    onTableOptionChange(payload) {
      dispatch(setListGameTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setListGameSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setListGameFilter({ selectedFilterKey }));
    },

    dispatchSetListGameView(view) {
      dispatch(setListGameView({ view }));
    },

    dispatchAddGames(ids, addOptions) {
      dispatch(addGames({ ids, addOptions }));
    },

    dispatchAddImportListExclusions(exclusions) {
      dispatch(addImportListExclusions(exclusions));
    },

    onImportListSyncPress() {
      dispatch(executeCommand({
        name: commandNames.IMPORT_LIST_SYNC,
        commandFinished: this.dispatchFetchListGames
      }));
    }
  };
}

class DiscoverGameConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.repopulate);
    this.props.dispatchFetchRootFolders();
    this.props.dispatchFetchListGames();
  }

  componentWillUnmount() {
    this.props.dispatchClearListGame();
    unregisterPagePopulator(this.repopulate);
  }

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetListGameView(view);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.discoverGame = scrollTop;
  };

  onAddGamesPress = ({ ids, addOptions }) => {
    this.props.dispatchAddGames(ids, addOptions);
  };

  onExcludeGamesPress =({ ids }) => {
    this.props.dispatchAddImportListExclusions({ ids });
  };

  //
  // Render

  render() {
    return (
      <DiscoverGame
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onAddGamesPress={this.onAddGamesPress}
        onExcludeGamesPress={this.onExcludeGamesPress}
        onSyncListsPress={this.onSyncListsPress}
      />
    );
  }
}

DiscoverGameConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchFetchRootFolders: PropTypes.func.isRequired,
  dispatchFetchListGames: PropTypes.func.isRequired,
  dispatchClearListGame: PropTypes.func.isRequired,
  dispatchSetListGameView: PropTypes.func.isRequired,
  dispatchAddGames: PropTypes.func.isRequired,
  dispatchAddImportListExclusions: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(DiscoverGameConnector),
  'discoverGame'
);
