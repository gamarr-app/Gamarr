import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createGameSelector from 'Store/Selectors/createGameSelector';
import ExtraFileTableContent from './ExtraFileTableContent';

function createMapStateToProps() {
  return createSelector(
    (state, { gameId }) => gameId,
    (state) => state.extraFiles,
    createGameSelector(),
    (
      gameId,
      extraFiles
    ) => {
      const filesForGame = extraFiles.items.filter((file) => file.gameId === gameId);

      return {
        items: filesForGame,
        error: null
      };
    }
  );
}

class ExtraFileTableContentConnector extends Component {

  //
  // Render

  render() {
    const {
      ...otherProps
    } = this.props;

    return (
      <ExtraFileTableContent
        {...otherProps}
      />
    );
  }
}

ExtraFileTableContentConnector.propTypes = {
  gameId: PropTypes.number.isRequired
};

export default connect(createMapStateToProps, null)(ExtraFileTableContentConnector);
