import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { queueLookupGame, setImportGameValue } from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import ImportGameRow from './ImportGameRow';

function createImportGameItemSelector() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.importGame.items,
    (id, items) => {
      return _.find(items, { id }) || {};
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createImportGameItemSelector(),
    createAllGamesSelector(),
    (item, games) => {
      const selectedGame = item && item.selectedGame;
      const isExistingGame = !!selectedGame && _.some(games, { igdbId: selectedGame.igdbId });

      return {
        ...item,
        isExistingGame
      };
    }
  );
}

const mapDispatchToProps = {
  queueLookupGame,
  setImportGameValue
};

class ImportGameRowConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setImportGameValue({
      id: this.props.id,
      [name]: value
    });
  };

  //
  // Render

  render() {
    // Don't show the row until we have the information we require for it.

    const {
      items,
      monitor
    } = this.props;

    if (!items || !monitor) {
      return null;
    }

    return (
      <ImportGameRow
        {...this.props}
        onInputChange={this.onInputChange}
        onGameSelect={this.onGameSelect}
      />
    );
  }
}

ImportGameRowConnector.propTypes = {
  rootFolderId: PropTypes.number.isRequired,
  id: PropTypes.string.isRequired,
  monitor: PropTypes.string,
  items: PropTypes.arrayOf(PropTypes.object),
  queueLookupGame: PropTypes.func.isRequired,
  setImportGameValue: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ImportGameRowConnector);
