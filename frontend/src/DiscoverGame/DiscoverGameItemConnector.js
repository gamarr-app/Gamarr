import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createDiscoverGameSelector from 'Store/Selectors/createDiscoverGameSelector';

function createMapStateToProps() {
  return createSelector(
    createDiscoverGameSelector(),
    (
      game
    ) => {

      // If a game is deleted this selector may fire before the parent
      // selectors, which will result in an undefined game, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show a game that has no information available.

      if (!game) {
        return {};
      }

      return {
        ...game
      };
    }
  );
}

class DiscoverGameItemConnector extends Component {

  //
  // Render

  render() {
    const {
      igdbId,
      component: ItemComponent,
      ...otherProps
    } = this.props;

    if (!igdbId) {
      return null;
    }

    return (
      <ItemComponent
        {...otherProps}
        igdbId={igdbId}
      />
    );
  }
}

DiscoverGameItemConnector.propTypes = {
  igdbId: PropTypes.number,
  component: PropTypes.elementType.isRequired
};

export default connect(createMapStateToProps)(DiscoverGameItemConnector);
