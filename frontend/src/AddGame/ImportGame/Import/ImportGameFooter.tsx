import _ from 'lodash';
import { Component } from 'react';
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

interface ImportGameFooterState {
  monitor: string | typeof MIXED;
  qualityProfileId: number | string | typeof MIXED;
  minimumAvailability: string | typeof MIXED;
}

type ImportGameFooterStateKey = keyof ImportGameFooterState;

class ImportGameFooter extends Component<
  ImportGameFooterProps,
  ImportGameFooterState
> {
  //
  // Lifecycle

  constructor(props: ImportGameFooterProps) {
    super(props);

    const {
      defaultMonitor,
      defaultQualityProfileId,
      defaultMinimumAvailability,
    } = props;

    this.state = {
      monitor: defaultMonitor,
      qualityProfileId: defaultQualityProfileId || 0,
      minimumAvailability: defaultMinimumAvailability || '',
    };
  }

  componentDidUpdate(_prevProps: ImportGameFooterProps) {
    const {
      defaultMonitor,
      defaultQualityProfileId,
      defaultMinimumAvailability,
      isMonitorMixed,
      isQualityProfileIdMixed,
      isMinimumAvailabilityMixed,
    } = this.props;

    const { monitor, qualityProfileId, minimumAvailability } = this.state;

    const newState: Partial<ImportGameFooterState> = {};

    if (isMonitorMixed && monitor !== MIXED) {
      newState.monitor = MIXED;
    } else if (!isMonitorMixed && monitor !== defaultMonitor) {
      newState.monitor = defaultMonitor;
    }

    if (isQualityProfileIdMixed && qualityProfileId !== MIXED) {
      newState.qualityProfileId = MIXED;
    } else if (
      !isQualityProfileIdMixed &&
      qualityProfileId !== defaultQualityProfileId
    ) {
      newState.qualityProfileId = defaultQualityProfileId || 0;
    }

    if (isMinimumAvailabilityMixed && minimumAvailability !== MIXED) {
      newState.minimumAvailability = MIXED;
    } else if (
      !isMinimumAvailabilityMixed &&
      minimumAvailability !== defaultMinimumAvailability
    ) {
      newState.minimumAvailability = defaultMinimumAvailability || '';
    }

    if (!_.isEmpty(newState)) {
      this.setState(newState as ImportGameFooterState);
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }: InputChanged) => {
    const key = name as ImportGameFooterStateKey;
    this.setState((prevState) => ({
      ...prevState,
      [key]: value,
    }));
    this.props.onInputChange({ name, value });
  };

  //
  // Render

  render() {
    const {
      selectedCount,
      isImporting,
      isLookingUpGame,
      isMonitorMixed,
      isQualityProfileIdMixed,
      isMinimumAvailabilityMixed,
      hasUnsearchedItems,
      importError,
      onImportPress,
      onLookupPress,
      onCancelLookupPress,
    } = this.props;

    const { monitor, qualityProfileId, minimumAvailability } = this.state;

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
            onChange={this.onInputChange}
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
            onChange={this.onInputChange}
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
            onChange={this.onInputChange}
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
}

export default ImportGameFooter;
