import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import FormInputGroup from 'Components/Form/FormInputGroup';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { inputTypes, kinds } from 'Helpers/Props';
import monitorOptions from 'Utilities/Game/monitorOptions';
import translate from 'Utilities/String/translate';
import DiscoverGameFooterLabel from './DiscoverGameFooterLabel';
import ExcludeGameModal from './Exclusion/ExcludeGameModal';
import styles from './DiscoverGameFooter.css';

class DiscoverGameFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    const {
      defaultMonitor,
      defaultQualityProfileId,
      defaultMinimumAvailability,
      defaultRootFolderPath,
      defaultSearchForGame
    } = props;

    this.state = {
      monitor: defaultMonitor,
      qualityProfileId: defaultQualityProfileId,
      minimumAvailability: defaultMinimumAvailability,
      rootFolderPath: defaultRootFolderPath,
      searchForGame: defaultSearchForGame,
      isExcludeGameModalOpen: false,
      destinationRootFolder: null
    };
  }

  componentDidUpdate(prevProps) {
    const {
      defaultMonitor,
      defaultQualityProfileId,
      defaultMinimumAvailability,
      defaultRootFolderPath,
      defaultSearchForGame
    } = this.props;

    const {
      monitor,
      qualityProfileId,
      minimumAvailability,
      rootFolderPath,
      searchForGame
    } = this.state;

    const newState = {};

    if (monitor !== defaultMonitor) {
      newState.monitor = defaultMonitor;
    }

    if (qualityProfileId !== defaultQualityProfileId) {
      newState.qualityProfileId = defaultQualityProfileId;
    }

    if (minimumAvailability !== defaultMinimumAvailability) {
      newState.minimumAvailability = defaultMinimumAvailability;
    }

    if (rootFolderPath !== defaultRootFolderPath) {
      newState.rootFolderPath = defaultRootFolderPath;
    }

    if (searchForGame !== defaultSearchForGame) {
      newState.searchForGame = defaultSearchForGame;
    }

    if (!_.isEmpty(newState)) {
      this.setState(newState);
    }
  }

  //
  // Listeners

  onExcludeSelectedPress = () => {
    this.setState({ isExcludeGameModalOpen: true });
  };

  onExcludeGameModalClose = () => {
    this.setState({ isExcludeGameModalOpen: false });
  };

  onAddGamesPress = () => {
    const {
      monitor,
      qualityProfileId,
      minimumAvailability,
      rootFolderPath,
      searchForGame
    } = this.state;

    const addOptions = {
      monitor,
      qualityProfileId,
      minimumAvailability,
      rootFolderPath,
      searchForGame
    };

    this.props.onAddGamesPress({ addOptions });
  };

  //
  // Render

  render() {
    const {
      selectedIds,
      selectedCount,
      isAdding,
      isExcluding,
      onInputChange
    } = this.props;

    const {
      monitor,
      qualityProfileId,
      minimumAvailability,
      rootFolderPath,
      searchForGame,
      isExcludeGameModalOpen
    } = this.state;

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
            value={qualityProfileId}
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
            value={minimumAvailability}
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
            value={rootFolderPath}
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
            value={searchForGame}
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
                  onPress={this.onAddGamesPress}
                >
                  {translate('AddGames')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.excludeSelectedButton}
                  kind={kinds.DANGER}
                  isSpinning={isExcluding}
                  isDisabled={!selectedCount || isExcluding}
                  onPress={this.props.onExcludeGamesPress}
                >
                  {translate('AddExclusion')}
                </SpinnerButton>
              </div>
            </div>
          </div>
        </div>

        <ExcludeGameModal
          isOpen={isExcludeGameModalOpen}
          gameIds={selectedIds}
          onModalClose={this.onExcludeGameModalClose}
        />
      </PageContentFooter>
    );
  }
}

DiscoverGameFooter.propTypes = {
  selectedIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isAdding: PropTypes.bool.isRequired,
  isExcluding: PropTypes.bool.isRequired,
  defaultMonitor: PropTypes.string.isRequired,
  defaultQualityProfileId: PropTypes.number,
  defaultMinimumAvailability: PropTypes.string,
  defaultRootFolderPath: PropTypes.string,
  defaultSearchForGame: PropTypes.bool,
  onInputChange: PropTypes.func.isRequired,
  onAddGamesPress: PropTypes.func.isRequired,
  onExcludeGamesPress: PropTypes.func.isRequired
};

export default DiscoverGameFooter;
