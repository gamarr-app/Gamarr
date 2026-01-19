import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearAddGame, lookupGame } from 'Store/Actions/addGameActions';
import { clearGameFiles, fetchGameFiles } from 'Store/Actions/gameFileActions';
import { clearQueueDetails, fetchQueueDetails } from 'Store/Actions/queueActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import hasDifferentItems from 'Utilities/Object/hasDifferentItems';
import selectUniqueIds from 'Utilities/Object/selectUniqueIds';
import parseUrl from 'Utilities/String/parseUrl';
import AddNewGame from './AddNewGame';

function createMapStateToProps() {
  return createSelector(
    (state) => state.addGame,
    (state) => state.games.items.length,
    (state) => state.router.location,
    (addGame, existingGamesCount, location) => {
      const { params } = parseUrl(location.search);

      return {
        ...addGame,
        term: params.term,
        hasExistingGames: existingGamesCount > 0
      };
    }
  );
}

const mapDispatchToProps = {
  lookupGame,
  clearAddGame,
  fetchRootFolders,
  fetchQueueDetails,
  clearQueueDetails,
  fetchGameFiles,
  clearGameFiles
};

class AddNewGameConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._gameLookupTimeout = null;
  }

  componentDidMount() {
    this.props.fetchRootFolders();
    this.props.fetchQueueDetails();
  }

  componentDidUpdate(prevProps) {
    const {
      items
    } = this.props;

    if (hasDifferentItems(prevProps.items, items)) {
      const gameIds = selectUniqueIds(items, 'internalId');

      if (gameIds.length) {
        this.props.fetchGameFiles({ gameId: gameIds });
      }
    }
  }

  componentWillUnmount() {
    if (this._gameLookupTimeout) {
      clearTimeout(this._gameLookupTimeout);
    }

    this.props.clearAddGame();
    this.props.clearQueueDetails();
    this.props.clearGameFiles();
  }

  //
  // Listeners

  onGameLookupChange = (term) => {
    if (this._gameLookupTimeout) {
      clearTimeout(this._gameLookupTimeout);
    }

    if (term.trim() === '') {
      this.props.clearAddGame();
    } else {
      this._gameLookupTimeout = setTimeout(() => {
        this.props.lookupGame({ term });
      }, 300);
    }
  };

  onClearGameLookup = () => {
    this.props.clearAddGame();
  };

  //
  // Render

  render() {
    const {
      term,
      ...otherProps
    } = this.props;

    return (
      <AddNewGame
        term={term}
        {...otherProps}
        onGameLookupChange={this.onGameLookupChange}
        onClearGameLookup={this.onClearGameLookup}
      />
    );
  }
}

AddNewGameConnector.propTypes = {
  term: PropTypes.string,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  lookupGame: PropTypes.func.isRequired,
  clearAddGame: PropTypes.func.isRequired,
  fetchRootFolders: PropTypes.func.isRequired,
  fetchQueueDetails: PropTypes.func.isRequired,
  clearQueueDetails: PropTypes.func.isRequired,
  fetchGameFiles: PropTypes.func.isRequired,
  clearGameFiles: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewGameConnector);
