import { Component } from 'react';
import { connect, ConnectedProps } from 'react-redux';
import { createSelector } from 'reselect';
import { Error } from 'App/State/AppSectionState';
import { Image } from 'Game/Game';
import {
  addGame,
  AddGameState,
  setAddGameDefault,
} from 'Store/Actions/addGameActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';
import { Failure } from 'typings/pending';
import AddNewGameModalContent from './AddNewGameModalContent';

interface FormValue<T> {
  value: T;
  errors?: Failure[];
  warnings?: Failure[];
}

interface AddGameAppState {
  addGame: AddGameState;
}

function createMapStateToProps() {
  return createSelector(
    (state: AddGameAppState) => state.addGame,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (addGameState, dimensions, systemStatus) => {
      const { isAdding, addError, defaults } = addGameState;

      const { settings } = selectSettings(
        defaults,
        {},
        addError as Error | undefined
      );

      return {
        isAdding,
        addError: addError as Error | undefined,
        isSmallScreen: dimensions.isSmallScreen,
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

const connector = connect(createMapStateToProps, mapDispatchToProps);

type PropsFromRedux = ConnectedProps<typeof connector>;

interface OwnProps {
  igdbId: number;
  steamAppId?: number;
  title: string;
  year: number;
  overview?: string;
  folder: string;
  images: Image[];
  onModalClose: () => void;
}

interface StateProps {
  isAdding: boolean;
  addError?: Error;
  isSmallScreen: boolean;
  isWindows: boolean;
  rootFolderPath?: FormValue<string>;
  monitor: FormValue<string>;
  qualityProfileId?: FormValue<number>;
  minimumAvailability: FormValue<string>;
  searchForGame: FormValue<boolean>;
  tags: FormValue<number[]>;
}

type AddNewGameModalContentConnectorProps = OwnProps &
  StateProps &
  PropsFromRedux;

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
    const {
      title,
      year,
      overview,
      images,
      isAdding,
      addError,
      rootFolderPath,
      monitor,
      qualityProfileId,
      minimumAvailability,
      searchForGame,
      folder,
      tags,
      isSmallScreen,
      isWindows,
      onModalClose,
    } = this.props;

    return (
      <AddNewGameModalContent
        title={title}
        year={year}
        overview={overview}
        images={images}
        isAdding={isAdding}
        addError={addError}
        rootFolderPath={rootFolderPath}
        monitor={monitor}
        qualityProfileId={qualityProfileId}
        minimumAvailability={minimumAvailability}
        searchForGame={searchForGame}
        folder={folder}
        tags={tags}
        isSmallScreen={isSmallScreen}
        isWindows={isWindows}
        onModalClose={onModalClose}
        onInputChange={this.onInputChange}
        onAddGamePress={this.onAddGamePress}
      />
    );
  }
}

export default connector(AddNewGameModalContentConnector);
