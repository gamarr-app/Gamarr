import { useCallback, useEffect, useState } from 'react';
import { ImportError } from 'App/State/ImportGameAppState';
import FormInputGroup from 'Components/Form/FormInputGroup';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContentFooter from 'Components/Page/PageContentFooter';
import Popover from 'Components/Tooltip/Popover';
import { GameMonitor } from 'Game/Game';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './ImportGameFooter.css';

const MIXED = 'mixed';

interface ImportGameFooterProps {
  selectedCount: number;
  isImporting: boolean;
  isLookingUpGame: boolean;
  defaultMonitor: GameMonitor;
  defaultQualityProfileId?: number;
  defaultMinimumAvailability?: string;
  isMonitorMixed: boolean;
  isQualityProfileIdMixed: boolean;
  isMinimumAvailabilityMixed: boolean;
  hasUnsearchedItems: boolean;
  importError?: ImportError;
  onInputChange: (change: InputChanged) => void;
  onImportPress: () => void;
  onLookupPress: () => void;
  onCancelLookupPress: () => void;
}

function ImportGameFooter(props: ImportGameFooterProps) {
  const {
    selectedCount,
    isImporting,
    isLookingUpGame,
    defaultMonitor,
    defaultQualityProfileId,
    defaultMinimumAvailability,
    isMonitorMixed,
    isQualityProfileIdMixed,
    isMinimumAvailabilityMixed,
    hasUnsearchedItems,
    importError,
    onInputChange,
    onImportPress,
    onLookupPress,
    onCancelLookupPress,
  } = props;

  const [monitor, setMonitor] = useState<string>(defaultMonitor);
  const [qualityProfileId, setQualityProfileId] = useState<number | string>(
    defaultQualityProfileId || 0
  );
  const [minimumAvailability, setMinimumAvailability] = useState<string>(
    defaultMinimumAvailability || ''
  );

  // Sync state with props when mixed states change
  useEffect(() => {
    if (isMonitorMixed && monitor !== MIXED) {
      setMonitor(MIXED);
    } else if (!isMonitorMixed && monitor !== defaultMonitor) {
      setMonitor(defaultMonitor);
    }
  }, [isMonitorMixed, defaultMonitor, monitor]);

  useEffect(() => {
    if (isQualityProfileIdMixed && qualityProfileId !== MIXED) {
      setQualityProfileId(MIXED);
    } else if (
      !isQualityProfileIdMixed &&
      qualityProfileId !== defaultQualityProfileId
    ) {
      setQualityProfileId(defaultQualityProfileId || 0);
    }
  }, [isQualityProfileIdMixed, defaultQualityProfileId, qualityProfileId]);

  useEffect(() => {
    if (isMinimumAvailabilityMixed && minimumAvailability !== MIXED) {
      setMinimumAvailability(MIXED);
    } else if (
      !isMinimumAvailabilityMixed &&
      minimumAvailability !== defaultMinimumAvailability
    ) {
      setMinimumAvailability(defaultMinimumAvailability || '');
    }
  }, [
    isMinimumAvailabilityMixed,
    defaultMinimumAvailability,
    minimumAvailability,
  ]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      if (name === 'monitor') {
        setMonitor(value as string);
      } else if (name === 'qualityProfileId') {
        setQualityProfileId(value as number | string);
      } else if (name === 'minimumAvailability') {
        setMinimumAvailability(value as string);
      }
      onInputChange({ name, value });
    },
    [onInputChange]
  );

  return (
    <PageContentFooter>
      <div className={styles.inputContainer}>
        <div className={styles.label}>{translate('Monitor')}</div>

        <FormInputGroup
          type={inputTypes.MONITOR_GAMES_SELECT}
          name="monitor"
          value={monitor}
          isDisabled={!selectedCount}
          includeMixed={isMonitorMixed}
          onChange={handleInputChange}
        />
      </div>

      <div className={styles.inputContainer}>
        <div className={styles.label}>{translate('MinimumAvailability')}</div>

        <FormInputGroup
          type={inputTypes.AVAILABILITY_SELECT}
          name="minimumAvailability"
          value={minimumAvailability}
          isDisabled={!selectedCount}
          includeMixed={isMinimumAvailabilityMixed}
          onChange={handleInputChange}
        />
      </div>

      <div className={styles.inputContainer}>
        <div className={styles.label}>{translate('QualityProfile')}</div>

        <FormInputGroup
          type={inputTypes.QUALITY_PROFILE_SELECT}
          name="qualityProfileId"
          value={qualityProfileId}
          isDisabled={!selectedCount}
          includeMixed={isQualityProfileIdMixed}
          onChange={handleInputChange}
        />
      </div>

      <div>
        <div className={styles.label}>&nbsp;</div>

        <div className={styles.importButtonContainer}>
          <SpinnerButton
            className={styles.importButton}
            kind={kinds.PRIMARY}
            isSpinning={isImporting}
            isDisabled={!selectedCount || isLookingUpGame}
            onPress={onImportPress}
          >
            {translate('Import')} {selectedCount}{' '}
            {selectedCount > 1 ? translate('Games') : translate('Game')}
          </SpinnerButton>

          {isLookingUpGame ? (
            <Button
              className={styles.loadingButton}
              kind={kinds.WARNING}
              onPress={onCancelLookupPress}
            >
              {translate('CancelProcessing')}
            </Button>
          ) : null}

          {hasUnsearchedItems ? (
            <Button
              className={styles.loadingButton}
              kind={kinds.SUCCESS}
              onPress={onLookupPress}
            >
              {translate('StartProcessing')}
            </Button>
          ) : null}

          {isLookingUpGame ? (
            <LoadingIndicator className={styles.loading} size={24} />
          ) : null}

          {isLookingUpGame ? translate('ProcessingFolders') : null}

          {importError ? (
            <Popover
              anchor={
                <Icon
                  className={styles.importError}
                  name={icons.WARNING}
                  kind={kinds.WARNING}
                />
              }
              title={translate('ImportErrors')}
              body={
                <ul>
                  {Array.isArray(importError.responseJSON) ? (
                    importError.responseJSON.map((error, index) => {
                      return <li key={index}>{error.errorMessage}</li>;
                    })
                  ) : (
                    <li>{JSON.stringify(importError.responseJSON)}</li>
                  )}
                </ul>
              }
              position={tooltipPositions.RIGHT}
            />
          ) : null}
        </div>
      </div>
    </PageContentFooter>
  );
}

export default ImportGameFooter;
