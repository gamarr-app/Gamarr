import { useCallback, useEffect, useState } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import FormInputGroup from 'Components/Form/FormInputGroup';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { inputTypes, kinds } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import monitorOptions from 'Utilities/Game/monitorOptions';
import translate from 'Utilities/String/translate';
import DiscoverGameFooterLabel from './DiscoverGameFooterLabel';
import ExcludeGameModal from './Exclusion/ExcludeGameModal';
import styles from './DiscoverGameFooter.css';

interface DiscoverGameFooterProps {
  selectedIds: number[];
  selectedCount: number;
  isAdding: boolean;
  isExcluding: boolean;
  defaultMonitor: string;
  defaultQualityProfileId?: number;
  defaultMinimumAvailability?: string;
  defaultRootFolderPath?: string;
  defaultSearchForGame?: boolean;
  onInputChange: (change: InputChanged) => void;
  onAddGamesPress: (options: { addOptions: AddOptions }) => void;
  onExcludeGamesPress: () => void;
}

interface AddOptions {
  monitor: string;
  qualityProfileId: number | undefined;
  minimumAvailability: string | undefined;
  rootFolderPath: string | undefined;
  searchForGame: boolean | undefined;
}

function DiscoverGameFooter({
  selectedIds,
  selectedCount,
  isAdding,
  isExcluding,
  defaultMonitor,
  defaultQualityProfileId,
  defaultMinimumAvailability,
  defaultRootFolderPath,
  defaultSearchForGame,
  onInputChange,
  onAddGamesPress,
  onExcludeGamesPress,
}: DiscoverGameFooterProps) {
  const [monitor, setMonitor] = useState(defaultMonitor);
  const [qualityProfileId, setQualityProfileId] = useState(
    defaultQualityProfileId
  );
  const [minimumAvailability, setMinimumAvailability] = useState(
    defaultMinimumAvailability
  );
  const [rootFolderPath, setRootFolderPath] = useState(defaultRootFolderPath);
  const [searchForGame, setSearchForGame] = useState(defaultSearchForGame);
  const [isExcludeGameModalOpen, setIsExcludeGameModalOpen] = useState(false);

  useEffect(() => {
    if (monitor !== defaultMonitor) {
      setMonitor(defaultMonitor);
    }

    if (qualityProfileId !== defaultQualityProfileId) {
      setQualityProfileId(defaultQualityProfileId);
    }

    if (minimumAvailability !== defaultMinimumAvailability) {
      setMinimumAvailability(defaultMinimumAvailability);
    }

    if (rootFolderPath !== defaultRootFolderPath) {
      setRootFolderPath(defaultRootFolderPath);
    }

    if (searchForGame !== defaultSearchForGame) {
      setSearchForGame(defaultSearchForGame);
    }
  }, [
    defaultMonitor,
    defaultQualityProfileId,
    defaultMinimumAvailability,
    defaultRootFolderPath,
    defaultSearchForGame,
    monitor,
    qualityProfileId,
    minimumAvailability,
    rootFolderPath,
    searchForGame,
  ]);

  const handleExcludeGameModalClose = useCallback(() => {
    setIsExcludeGameModalOpen(false);
  }, []);

  const handleAddGamesPress = useCallback(() => {
    const addOptions: AddOptions = {
      monitor,
      qualityProfileId,
      minimumAvailability,
      rootFolderPath,
      searchForGame,
    };

    onAddGamesPress({ addOptions });
  }, [
    monitor,
    qualityProfileId,
    minimumAvailability,
    rootFolderPath,
    searchForGame,
    onAddGamesPress,
  ]);

  return (
    <PageContentFooter>
      <div className={styles.inputContainer}>
        <DiscoverGameFooterLabel
          label={translate('MonitorGame')}
          isSaving={isAdding}
        />

        <FormInputGroup
          type={inputTypes.SELECT}
          name="monitor"
          value={monitor}
          values={monitorOptions}
          isDisabled={!selectedCount}
          onChange={onInputChange}
        />
      </div>

      <div className={styles.inputContainer}>
        <DiscoverGameFooterLabel
          label={translate('QualityProfile')}
          isSaving={isAdding}
        />

        <FormInputGroup
          type={inputTypes.QUALITY_PROFILE_SELECT}
          name="qualityProfileId"
          value={qualityProfileId ?? 0}
          isDisabled={!selectedCount}
          onChange={onInputChange}
        />
      </div>

      <div className={styles.inputContainer}>
        <DiscoverGameFooterLabel
          label={translate('MinimumAvailability')}
          isSaving={isAdding}
        />

        <FormInputGroup
          type={inputTypes.AVAILABILITY_SELECT}
          name="minimumAvailability"
          value={minimumAvailability ?? ''}
          isDisabled={!selectedCount}
          onChange={onInputChange}
        />
      </div>

      <div className={styles.inputContainer}>
        <DiscoverGameFooterLabel
          label={translate('RootFolder')}
          isSaving={isAdding}
        />

        <FormInputGroup
          type={inputTypes.ROOT_FOLDER_SELECT}
          name="rootFolderPath"
          value={rootFolderPath ?? ''}
          isDisabled={!selectedCount}
          selectedValueOptions={{ includeFreeSpace: false }}
          onChange={onInputChange}
        />
      </div>

      <div className={styles.inputContainer}>
        <DiscoverGameFooterLabel
          label={translate('SearchOnAdd')}
          isSaving={isAdding}
        />

        <CheckInput
          name="searchForGame"
          isDisabled={!selectedCount}
          value={searchForGame ?? false}
          onChange={onInputChange}
        />
      </div>

      <div className={styles.buttonContainer}>
        <div className={styles.buttonContainerContent}>
          <DiscoverGameFooterLabel
            label={translate('GamesSelectedInterp', { count: selectedCount })}
            isSaving={false}
          />

          <div className={styles.buttons}>
            <div>
              <SpinnerButton
                className={styles.addSelectedButton}
                kind={kinds.PRIMARY}
                isSpinning={isAdding}
                isDisabled={!selectedCount || isAdding}
                onPress={handleAddGamesPress}
              >
                {translate('AddGames')}
              </SpinnerButton>

              <SpinnerButton
                className={styles.excludeSelectedButton}
                kind={kinds.DANGER}
                isSpinning={isExcluding}
                isDisabled={!selectedCount || isExcluding}
                onPress={onExcludeGamesPress}
              >
                {translate('AddExclusion')}
              </SpinnerButton>
            </div>
          </div>
        </div>
      </div>

      <ExcludeGameModal
        isOpen={isExcludeGameModalOpen}
        igdbId={selectedIds[0] || 0}
        title=""
        onModalClose={handleExcludeGameModalClose}
      />
    </PageContentFooter>
  );
}

export default DiscoverGameFooter;
