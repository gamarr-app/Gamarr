import { Component } from 'react';
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
import { Image } from 'Game/Game';
import GamePoster from 'Game/GamePoster';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { Failure } from 'typings/pending';
import translate from 'Utilities/String/translate';
import styles from './AddNewGameModalContent.css';

export interface FormValue<T> {
  value: T;
  errors?: Failure[];
  warnings?: Failure[];
}

interface AddNewGameModalContentProps {
  title: string;
  year: number;
  overview?: string;
  images: Image[];
  isAdding: boolean;
  addError?: object;
  rootFolderPath?: FormValue<string>;
  monitor: FormValue<string>;
  qualityProfileId?: FormValue<number>;
  minimumAvailability: FormValue<string>;
  searchForGame: FormValue<boolean>;
  folder: string;
  tags: FormValue<number[]>;
  isSmallScreen: boolean;
  isWindows: boolean;
  onModalClose: () => void;
  onInputChange: (change: InputChanged) => void;
  onAddGamePress: () => void;
}

class AddNewGameModalContent extends Component<AddNewGameModalContentProps> {
  //
  // Listeners

  onQualityProfileIdChange = ({
    name,
    value,
  }: {
    name: string;
    value: string | number;
  }) => {
    this.props.onInputChange({
      name,
      value: typeof value === 'string' ? parseInt(value) : value,
    });
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
      onInputChange,
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {title}

          {!title.contains(String(year)) && !!year && (
            <span className={styles.year}>({year})</span>
          )}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {!isSmallScreen && (
              <div className={styles.poster}>
                <GamePoster
                  className={styles.poster}
                  images={images}
                  size={250}
                />
              </div>
            )}

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
                      isWindows,
                    }}
                    selectedValueOptions={{
                      gameFolder: folder,
                      isWindows,
                    }}
                    helpText={translate('AddNewGameRootFolderHelpText', {
                      folder,
                    })}
                    onChange={onInputChange}
                    {...rootFolderPath}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>{translate('Monitor')}</FormLabel>

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
                        <Icon className={styles.labelIcon} name={icons.INFO} />
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
                    value={qualityProfileId?.value ?? 0}
                    errors={qualityProfileId?.errors}
                    warnings={qualityProfileId?.warnings}
                    onChange={this.onQualityProfileIdChange}
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

export default AddNewGameModalContent;
