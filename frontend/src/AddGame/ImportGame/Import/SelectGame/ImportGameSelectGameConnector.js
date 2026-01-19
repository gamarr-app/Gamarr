import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { queueLookupGame, setImportGameValue } from 'Store/Actions/importGameActions';
import createImportGameItemSelector from 'Store/Selectors/createImportGameItemSelector';
import ImportGameSelectGame from './ImportGameSelectGame';

function createMapStateToProps() {
  return createSelector(
    (state) => state.importGame.isLookingUpGame,
    createImportGameItemSelector(),
    (isLookingUpGame, item) => {
      return {
        isLookingUpGame,
        ...item
      };
    }
  );
}

const mapDispatchToProps = {
  queueLookupGame,
  setImportGameValue
};

class ImportGameSelectGameConnector extends Component {

  //
  // Listeners

  onSearchInputChange = (term) => {
    this.props.queueLookupGame({
      name: this.props.id,
      term,
      topOfQueue: true
    });
  };

  onGameSelect = (igdbId) => {
    const {
      id,
      items
    } = this.props;

    this.props.setImportGameValue({
      id,
      selectedGame: _.find(items, { igdbId })
    });
  };

  //
  // Render

  render() {
    return (
      <ImportGameSelectGame
        {...this.props}
        onSearchInputChange={this.onSearchInputChange}
        onGameSelect={this.onGameSelect}
      />
    );
  }
}

ImportGameSelectGameConnector.propTypes = {
  id: PropTypes.string.isRequired,
  items: PropTypes.arrayOf(PropTypes.object),
  selectedGame: PropTypes.object,
  isSelected: PropTypes.bool,
  queueLookupGame: PropTypes.func.isRequired,
  setImportGameValue: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ImportGameSelectGameConnector);
