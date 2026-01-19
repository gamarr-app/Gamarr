import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setAddGameDefault } from 'Store/Actions/discoverGameActions';
import DiscoverGameFooter from './DiscoverGameFooter';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame,
    (state) => state.settings.importListExclusions,
    (state, { selectedIds }) => selectedIds,
    (discoverGame, importListExclusions, selectedIds) => {
      const {
        monitor: defaultMonitor,
        qualityProfileId: defaultQualityProfileId,
        minimumAvailability: defaultMinimumAvailability,
        rootFolderPath: defaultRootFolderPath,
        searchForGame: defaultSearchForGame
      } = discoverGame.defaults;

      const {
        isAdding
      } = discoverGame;

      const {
        isSaving
      } = importListExclusions;

      return {
        selectedCount: selectedIds.length,
        isAdding,
        isExcluding: isSaving,
        defaultMonitor,
        defaultQualityProfileId,
        defaultMinimumAvailability,
        defaultRootFolderPath,
        defaultSearchForGame
      };
    }
  );
}

const mapDispatchToProps = {
  setAddGameDefault
};

class DiscoverGameFooterConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setAddGameDefault({ [name]: value });
  };

  //
  // Render

  render() {
    return (
      <DiscoverGameFooter
        {...this.props}
        onInputChange={this.onInputChange}
      />
    );
  }
}

DiscoverGameFooterConnector.propTypes = {
  setAddGameDefault: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DiscoverGameFooterConnector);
