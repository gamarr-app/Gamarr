import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleGameMonitored } from 'Store/Actions/gameActions';
import createCollectionExistingGameSelector from 'Store/Selectors/createCollectionExistingGameSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import CollectionGame from './CollectionGame';

function createMapStateToProps() {
  return createSelector(
    createDimensionsSelector(),
    createCollectionExistingGameSelector(),
    (dimensions, existingGame) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        isExistingGame: !!existingGame,
        ...existingGame
      };
    }
  );
}

const mapDispatchToProps = {
  toggleGameMonitored
};

class CollectionGameConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleGameMonitored({
      gameId: this.props.id,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <CollectionGame
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
      />
    );
  }
}

CollectionGameConnector.propTypes = {
  id: PropTypes.number,
  monitored: PropTypes.bool,
  toggleGameMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(CollectionGameConnector);
