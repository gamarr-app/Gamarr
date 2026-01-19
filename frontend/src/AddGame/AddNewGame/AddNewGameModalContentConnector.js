import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { addGame, setAddGameDefault } from 'Store/Actions/addGameActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import AddNewGameModalContent from './AddNewGameModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.addGame,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (addGameState, dimensions, systemStatus) => {
      const {
        isAdding,
        addError,
        defaults
      } = addGameState;

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

class AddNewGameModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setAddGameDefault({ [name]: value });
  };

  onAddGamePress = () => {
    const {
      igdbId,
      steamAppId,
      rootFolderPath,
      monitor,
      qualityProfileId,
      minimumAvailability,
      searchForGame,
      tags
    } = this.props;

    this.props.addGame({
      igdbId,
      steamAppId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      qualityProfileId: qualityProfileId.value,
      minimumAvailability: minimumAvailability.value,
      searchForGame: searchForGame.value,
      tags: tags.value
    });
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

AddNewGameModalContentConnector.propTypes = {
  igdbId: PropTypes.number.isRequired,
  steamAppId: PropTypes.number,
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

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewGameModalContentConnector);
