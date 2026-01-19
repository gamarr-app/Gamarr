import PropTypes from 'prop-types';
import React, { Component } from 'react';
import GameMinimumAvailabilityPopoverContent from 'AddGame/GameMinimumAvailabilityPopoverContent';
import CheckInput from 'Components/Form/CheckInput';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import GamePoster from 'Game/GamePoster';
import translate from 'Utilities/String/translate';
import styles from './AddNewGameModalContent.css';

class AddNewGameModalContent extends Component {

  //
  // Listeners

  onQualityProfileIdChange = ({ value }) => {
    this.props.onInputChange({ name: 'qualityProfileId', value: parseInt(value) });
  };

  onAddGamePress = () => {
    this.props.onAddGamePress();
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
      onInputChange
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {title}

          {
            !title.contains(year) && !!year &&
              <span className={styles.year}>({year})</span>
          }
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              !isSmallScreen &&
                <div className={styles.poster}>
                  <GamePoster
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              {overview ? (
                <div className={styles.overview}>{overview}</div>
              ) : null}

              <Form>
                <FormGroup>
                  <FormLabel>{translate('RootFolder')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.ROOT_FOLDER_SELECT}
                    name="rootFolderPath"
                    valueOptions={{
                      gameFolder: folder,
                      isWindows
                    }}
                    selectedValueOptions={{
                      gameFolder: folder,
                      isWindows
                    }}
                    helpText={translate('AddNewGameRootFolderHelpText', {
                      folder
                    })}
                    onChange={onInputChange}
                    {...rootFolderPath}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>
                    {translate('Monitor')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.MONITOR_GAMES_SELECT}
                    name="monitor"
                    onChange={onInputChange}
                    {...monitor}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>
                    {translate('MinimumAvailability')}

                    <Popover
                      anchor={
                        <Icon
                          className={styles.labelIcon}
                          name={icons.INFO}
                        />
                      }
                      title={translate('MinimumAvailability')}
                      body={<GameMinimumAvailabilityPopoverContent />}
                      position={tooltipPositions.RIGHT}
                    />
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.AVAILABILITY_SELECT}
                    name="minimumAvailability"
                    onChange={onInputChange}
                    {...minimumAvailability}
                    helpLink="https://wiki.servarr.com/gamarr/faq#what-is-minimum-availability"
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>{translate('QualityProfile')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.QUALITY_PROFILE_SELECT}
                    name="qualityProfileId"
                    onChange={this.onQualityProfileIdChange}
                    {...qualityProfileId}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>{translate('Tags')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.TAG}
                    name="tags"
                    onChange={onInputChange}
                    {...tags}
                  />
                </FormGroup>
              </Form>
            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForMissingGameLabelContainer}>
            <span className={styles.searchForMissingGameLabel}>
              {translate('StartSearchForMissingGame')}
            </span>

            <CheckInput
              containerClassName={styles.searchForMissingGameContainer}
              className={styles.searchForMissingGameInput}
              name="searchForGame"
              onChange={onInputChange}
              {...searchForGame}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddGamePress}
          >
            {translate('AddGame')}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewGameModalContent.propTypes = {
  title: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  minimumAvailability: PropTypes.object.isRequired,
  searchForGame: PropTypes.object.isRequired,
  folder: PropTypes.string.isRequired,
  tags: PropTypes.object.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isWindows: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onAddGamePress: PropTypes.func.isRequired
};

export default AddNewGameModalContent;
