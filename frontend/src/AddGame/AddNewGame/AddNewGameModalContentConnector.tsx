import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
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

const addNewGameModalContentSelector = createSelector(
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

interface AddNewGameModalContentConnectorProps {
  igdbId: number;
  steamAppId?: number;
  title: string;
  year: number;
  overview?: string;
  folder: string;
  images: Image[];
  onModalClose: () => void;
}

function AddNewGameModalContentConnector(
  props: AddNewGameModalContentConnectorProps
) {
  const {
    igdbId,
    steamAppId,
    title,
    year,
    overview,
    folder,
    images,
    onModalClose,
  } = props;

  const dispatch = useDispatch();

  const {
    isAdding,
    addError,
    isSmallScreen,
    isWindows,
    rootFolderPath,
    monitor,
    monitorUpdates,
    qualityProfileId,
    minimumAvailability,
    searchForGame,
    tags,
    platform,
  } = useSelector(addNewGameModalContentSelector) as {
    isAdding: boolean;
    addError?: Error;
    isSmallScreen: boolean;
    isWindows: boolean;
    rootFolderPath?: FormValue<string>;
    monitor: FormValue<string>;
    monitorUpdates: FormValue<boolean>;
    qualityProfileId?: FormValue<number>;
    minimumAvailability: FormValue<string>;
    searchForGame: FormValue<boolean>;
    tags: FormValue<number[]>;
    platform?: FormValue<string>;
  };

  const onInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setAddGameDefault({ [name]: value }));
    },
    [dispatch]
  );

  const onAddGamePress = useCallback(() => {
    dispatch(
      addGame({
        igdbId,
        steamAppId,
        rootFolderPath: rootFolderPath?.value || '',
        monitor: monitor.value,
        monitorUpdates: monitorUpdates.value,
        qualityProfileId: qualityProfileId?.value || 0,
        minimumAvailability: minimumAvailability.value,
        searchForGame: searchForGame.value,
        tags: tags.value,
        platform: platform?.value ?? 'unknown',
      })
    );
  }, [
    dispatch,
    igdbId,
    steamAppId,
    rootFolderPath,
    monitor,
    monitorUpdates,
    qualityProfileId,
    minimumAvailability,
    searchForGame,
    tags,
    platform,
  ]);

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
      monitorUpdates={monitorUpdates}
      qualityProfileId={qualityProfileId}
      minimumAvailability={minimumAvailability}
      searchForGame={searchForGame}
      platform={platform}
      folder={folder}
      tags={tags}
      isSmallScreen={isSmallScreen}
      isWindows={isWindows}
      onModalClose={onModalClose}
      onInputChange={onInputChange}
      onAddGamePress={onAddGamePress}
    />
  );
}

export default AddNewGameModalContentConnector;
