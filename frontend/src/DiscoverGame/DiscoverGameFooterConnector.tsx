import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { setAddGameDefault } from 'Store/Actions/discoverGameActions';
import { InputChanged } from 'typings/inputs';
import DiscoverGameFooter from './DiscoverGameFooter';

interface DiscoverGameDefaults {
  monitor: string;
  qualityProfileId: number;
  minimumAvailability: string;
  rootFolderPath: string;
  searchForGame: boolean;
}

interface DiscoverGameState {
  isAdding: boolean;
  defaults: DiscoverGameDefaults;
}

interface ImportListExclusionsState {
  isSaving: boolean;
}

interface SettingsState {
  importListExclusions: ImportListExclusionsState;
}

interface AppState {
  discoverGame: DiscoverGameState;
  settings: SettingsState;
}

interface OwnProps {
  selectedIds: number[];
  onAddGamesPress: (options: {
    addOptions: {
      monitor: string;
      qualityProfileId?: number;
      minimumAvailability?: string;
      rootFolderPath?: string;
      searchForGame?: boolean;
    };
  }) => void;
  onExcludeGamesPress: () => void;
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.discoverGame,
    (state: AppState) => state.settings.importListExclusions,
    (_state: AppState, { selectedIds }: OwnProps) => selectedIds,
    (discoverGame, importListExclusions, selectedIds) => {
      const {
        monitor: defaultMonitor,
        qualityProfileId: defaultQualityProfileId,
        minimumAvailability: defaultMinimumAvailability,
        rootFolderPath: defaultRootFolderPath,
        searchForGame: defaultSearchForGame,
      } = discoverGame.defaults;

      const { isAdding } = discoverGame;

      const { isSaving } = importListExclusions;

      return {
        selectedCount: selectedIds.length,
        isAdding,
        isExcluding: isSaving,
        defaultMonitor,
        defaultQualityProfileId,
        defaultMinimumAvailability,
        defaultRootFolderPath,
        defaultSearchForGame,
      };
    }
  );
}

function DiscoverGameFooterConnector({
  selectedIds,
  onAddGamesPress,
  onExcludeGamesPress,
}: OwnProps) {
  const dispatch = useDispatch();

  const mapStateToPropsSelector = createMapStateToProps();
  const stateProps = useSelector((state: AppState) =>
    mapStateToPropsSelector(state, {
      selectedIds,
      onAddGamesPress,
      onExcludeGamesPress,
    })
  );

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setAddGameDefault({ [name]: value }));
    },
    [dispatch]
  );

  return (
    <DiscoverGameFooter
      {...stateProps}
      selectedIds={selectedIds}
      onInputChange={handleInputChange}
      onAddGamesPress={onAddGamesPress}
      onExcludeGamesPress={onExcludeGamesPress}
    />
  );
}

export default DiscoverGameFooterConnector;
