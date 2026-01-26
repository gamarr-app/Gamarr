import { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AddNewGameModalContent from 'AddGame/AddNewGame/AddNewGameModalContent';
import { Error } from 'App/State/AppSectionState';
import { Image } from 'Game/Game';
import { addGame, setAddGameDefault } from 'Store/Actions/discoverGameActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';

interface DiscoverGameDefaults {
  rootFolderPath: string;
  monitor: string;
  qualityProfileId: number;
  minimumAvailability: string;
  searchForGame: boolean;
  tags: number[];
}

interface DiscoverGameState {
  isAdding: boolean;
  addError: Error | undefined;
  defaults: DiscoverGameDefaults;
}

interface AppState {
  discoverGame: DiscoverGameState;
}

interface AddNewDiscoverGameModalContentConnectorProps {
  igdbId: number;
  title: string;
  year?: number;
  overview?: string;
  images?: Image[];
  folder?: string;
  onModalClose: (didAdd?: boolean) => void;
}

function AddNewDiscoverGameModalContentConnector({
  igdbId,
  title,
  year,
  overview,
  images,
  folder = '',
  onModalClose,
}: AddNewDiscoverGameModalContentConnectorProps) {
  const dispatch = useDispatch();

  const mapStateSelector = useMemo(
    () =>
      createSelector(
        (state: AppState) => state.discoverGame,
        createDimensionsSelector(),
        createSystemStatusSelector(),
        (discoverGameState, dimensions, systemStatus) => {
          const { isAdding, addError, defaults } = discoverGameState;

          const { settings } = selectSettings(defaults, {}, addError);

          return {
            isAdding,
            addError,
            isSmallScreen: dimensions.isSmallScreen,
            isWindows: systemStatus.isWindows,
            settings,
          };
        }
      ),
    []
  );

  const selectorResult = useSelector(mapStateSelector);

  const { isAdding, isSmallScreen, isWindows, settings } = selectorResult;

  // selectSettings returns PendingSection<DiscoverGameDefaults> which has the same
  // shape as FormValue (value, errors, warnings) for each property
  const {
    rootFolderPath,
    monitor,
    qualityProfileId,
    minimumAvailability,
    searchForGame,
    tags,
  } = settings;

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setAddGameDefault({ [name]: value }));
    },
    [dispatch]
  );

  const handleAddGamePress = useCallback(() => {
    dispatch(
      addGame({
        igdbId,
        rootFolderPath: rootFolderPath.value,
        monitor: monitor.value,
        qualityProfileId: qualityProfileId.value,
        minimumAvailability: minimumAvailability.value,
        searchForGame: searchForGame.value,
        tags: tags.value,
      })
    );

    onModalClose(true);
  }, [
    dispatch,
    igdbId,
    rootFolderPath,
    monitor,
    qualityProfileId,
    minimumAvailability,
    searchForGame,
    tags,
    onModalClose,
  ]);

  return (
    <AddNewGameModalContent
      title={title}
      year={year || 0}
      overview={overview}
      images={images || []}
      isAdding={isAdding}
      isSmallScreen={isSmallScreen}
      isWindows={isWindows}
      rootFolderPath={rootFolderPath}
      monitor={monitor}
      qualityProfileId={qualityProfileId}
      minimumAvailability={minimumAvailability}
      searchForGame={searchForGame}
      tags={tags}
      folder={folder}
      onModalClose={onModalClose}
      onInputChange={handleInputChange}
      onAddGamePress={handleAddGamePress}
    />
  );
}

export default AddNewDiscoverGameModalContentConnector;
