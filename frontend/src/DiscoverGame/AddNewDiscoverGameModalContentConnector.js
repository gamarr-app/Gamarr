import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AddNewGameModalContent from 'AddGame/AddNewGame/AddNewGameModalContent';
import { addGame, setAddGameDefault } from 'Store/Actions/discoverGameActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (discoverGameState, dimensions, systemStatus) => {
      const {
        isAdding,
        addError,
        defaults
      } = discoverGameState;

      const {
        settings,
        validationErrors,
        validationWarnings
      } = selectSettings(defaults, {}, addError);

      return {
        isAdding,
        addError,
        isSmallScreen: dimensions.isSmallScreen,
        validationErrors,
        validationWarnings,
        isWindows: systemStatus.isWindows,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  setAddGameDefault,
  addGame
};

class AddNewDiscoverGameModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setAddGameDefault({ [name]: value });
  };

  onAddGamePress = () => {
    const {
      igdbId,
      rootFolderPath,
      monitor,
      qualityProfileId,
      minimumAvailability,
      searchForGame,
      tags
    } = this.props;

    this.props.addGame({
      igdbId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      qualityProfileId: qualityProfileId.value,
      minimumAvailability: minimumAvailability.value,
      searchForGame: searchForGame.value,
      tags: tags.value
    });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <AddNewGameModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddGamePress={this.onAddGamePress}
      />
    );
  }
}

AddNewDiscoverGameModalContentConnector.propTypes = {
  igdbId: PropTypes.number.isRequired,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  minimumAvailability: PropTypes.object.isRequired,
  searchForGame: PropTypes.object.isRequired,
  tags: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired,
  setAddGameDefault: PropTypes.func.isRequired,
  addGame: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewDiscoverGameModalContentConnector);
