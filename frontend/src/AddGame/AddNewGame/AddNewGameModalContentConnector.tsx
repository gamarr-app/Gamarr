import { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { Error } from 'App/State/AppSectionState';
import { GamePlatform, Image } from 'Game/Game';
import { getUnambiguousPlatform } from 'Game/platformOptions';
import {
  addGame,
  AddGameState,
  setAddGameDefault,
} from 'Store/Actions/addGameActions';
import {
  fetchPlatformRootFolders,
  PlatformRootFolderItem,
} from 'Store/Actions/settingsActions';
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
  platforms?: GamePlatform[];
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
    platforms,
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

  const platformRootFolders = useSelector(
    (state: {
      settings: {
        platformRootFolders: {
          isPopulated: boolean;
          items: PlatformRootFolderItem[];
        };
      };
    }) => state.settings.platformRootFolders
  );

  useEffect(() => {
    dispatch(fetchPlatformRootFolders());
  }, [dispatch]);

  // The root folder stays a per-add choice, this only pre-fills it: a Switch
  // exclusive lands on the Switch default instead of whatever was used last.
  // The platform the user picked wins over the one derived from metadata,
  // which is the same precedence AddGameService applies server side.
  const effectivePlatform =
    platform?.value && platform.value !== 'unknown'
      ? platform.value
      : getUnambiguousPlatform(platforms);

  // Applied once per platform so that changing the root folder by hand
  // afterwards sticks (and so a default pointing at a removed root folder
  // can't ping-pong with RootFolderSelectInput's own fallback).
  const appliedPlatformRef = useRef<string | null>(null);

  useEffect(() => {
    if (!platformRootFolders.isPopulated) {
      return;
    }

    if (appliedPlatformRef.current === effectivePlatform) {
      return;
    }

    appliedPlatformRef.current = effectivePlatform;

    const defaultRootFolder =
      platformRootFolders.items.find((i) => i.platform === effectivePlatform) ??
      platformRootFolders.items.find((i) => i.platform === 'unknown');

    if (defaultRootFolder) {
      dispatch(setAddGameDefault({ rootFolderPath: defaultRootFolder.path }));
    }
  }, [dispatch, effectivePlatform, platformRootFolders]);

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
