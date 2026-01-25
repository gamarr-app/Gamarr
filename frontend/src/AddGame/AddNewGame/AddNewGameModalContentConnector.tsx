import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { Image } from 'Game/Game';
import { addGame, setAddGameDefault } from 'Store/Actions/addGameActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';
import AddNewGameModalContent from './AddNewGameModalContent';

interface FormValue<T> {
  value: T;
  errors?: { message: string }[];
  warnings?: { message: string }[];
}

function createMapStateToProps() {
  return createSelector(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.addGame,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (addGameState, dimensions, systemStatus) => {
      const { isAdding, addError, defaults } = addGameState;

      const { settings, validationErrors, validationWarnings } = selectSettings(
        defaults,
        {},
        addError
      );

      return {
        isAdding,
        addError,
        isSmallScreen: dimensions.isSmallScreen,
        validationErrors,
        validationWarnings,
        isWindows: systemStatus.isWindows,
        ...settings,
      };
    }
  );
}

const mapDispatchToProps = {
  setAddGameDefault,
  addGame,
};

interface AddNewGameModalContentConnectorProps {
  igdbId: number;
  steamAppId?: number;
  title: string;
  year: number;
  overview?: string;
  folder: string;
  images: Image[];
  rootFolderPath?: FormValue<string>;
  monitor: FormValue<string>;
  qualityProfileId?: FormValue<number>;
  minimumAvailability: FormValue<string>;
  searchForGame: FormValue<boolean>;
  tags: FormValue<number[]>;
  onModalClose: () => void;
  setAddGameDefault: (defaults: Record<string, unknown>) => void;
  addGame: (payload: {
    igdbId: number;
    steamAppId?: number;
    rootFolderPath: string;
    monitor: string;
    qualityProfileId: number;
    minimumAvailability: string;
    searchForGame: boolean;
    tags: number[];
  }) => void;
}

class AddNewGameModalContentConnector extends Component<AddNewGameModalContentConnectorProps> {
  //
  // Listeners

  onInputChange = ({ name, value }: InputChanged) => {
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
      tags,
    } = this.props;

    this.props.addGame({
      igdbId,
      steamAppId,
      rootFolderPath: rootFolderPath?.value || '',
      monitor: monitor.value,
      qualityProfileId: qualityProfileId?.value || 0,
      minimumAvailability: minimumAvailability.value,
      searchForGame: searchForGame.value,
      tags: tags.value,
    });
  };

  //
  // Render

  render() {
    return (
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      <AddNewGameModalContent
        {...(this.props as any)}
        onInputChange={this.onInputChange}
        onAddGamePress={this.onAddGamePress}
      />
    );
  }
}

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(AddNewGameModalContentConnector);
