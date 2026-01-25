import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import GameMinimumAvailabilityPopoverContent from 'AddGame/GameMinimumAvailabilityPopoverContent';
import AppState from 'App/State/AppState';
import useGameCollection from 'Collection/useGameCollection';
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
import usePrevious from 'Helpers/Hooks/usePrevious';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import {
  addGame,
  setGameCollectionValue,
} from 'Store/Actions/gameCollectionActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import useIsWindows from 'System/useIsWindows';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './AddNewGameCollectionGameModalContent.css';

export interface AddNewGameCollectionGameModalContentProps {
  igdbId: number;
  title: string;
  year: number;
  overview?: string;
  images: Image[];
  collectionId: number;
  folder: string;
  onModalClose: () => void;
}

function AddNewGameCollectionGameModalContent({
  igdbId,
  title,
  year,
  overview,
  images,
  collectionId,
  folder,
  onModalClose,
}: AddNewGameCollectionGameModalContentProps) {
  const dispatch = useDispatch();

  const collection = useGameCollection(collectionId)!;

  const { isSmallScreen } = useSelector(createDimensionsSelector());
  const isWindows = useIsWindows();

  const { isAdding, addError, pendingChanges } = useSelector(
    (state: AppState) => state.gameCollections
  );

  const wasAdding = usePrevious(isAdding);

  const { settings, validationErrors, validationWarnings } = useMemo(() => {
    const options = {
      rootFolderPath: collection.rootFolderPath,
      monitor: collection.monitored ? 'gameOnly' : 'none',
      qualityProfileId: collection.qualityProfileId,
      minimumAvailability: collection.minimumAvailability,
      searchForGame: collection.searchOnAdd,
      tags: collection.tags || [],
    };

    return selectSettings(options, pendingChanges, addError);
  }, [collection, pendingChanges, addError]);

  const {
    monitor,
    qualityProfileId,
    minimumAvailability,
    rootFolderPath,
    searchForGame,
    tags,
  } = settings;

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setGameCollectionValue({ name, value }));
    },
    [dispatch]
  );

  const handleAddGamePress = useCallback(() => {
    dispatch(
      addGame({
        igdbId,
        title,
        rootFolderPath: rootFolderPath.value,
        monitor: monitor.value,
        qualityProfileId: qualityProfileId.value,
        minimumAvailability: minimumAvailability.value,
        searchForGame: searchForGame.value,
        tags: tags.value,
      })
    );
  }, [
    igdbId,
    title,
    rootFolderPath,
    monitor,
    qualityProfileId,
    minimumAvailability,
    searchForGame,
    tags,
    dispatch,
  ]);

  useEffect(() => {
    if (!isAdding && wasAdding && !addError) {
      onModalClose();
    }
  }, [isAdding, wasAdding, addError, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {title}

        {!title.includes(String(year)) && year ? (
          <span className={styles.year}>({year})</span>
        ) : null}
      </ModalHeader>

      <ModalBody>
        <div className={styles.container}>
          {isSmallScreen ? null : (
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

            <Form
              validationErrors={validationErrors}
              validationWarnings={validationWarnings}
            >
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
                  {...rootFolderPath}
                  onChange={handleInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('Monitor')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.MONITOR_GAMES_SELECT}
                  name="monitor"
                  {...monitor}
                  onChange={handleInputChange}
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
                  {...minimumAvailability}
                  helpLink="https://wiki.servarr.com/gamarr/faq#what-is-minimum-availability"
                  onChange={handleInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('QualityProfile')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.QUALITY_PROFILE_SELECT}
                  name="qualityProfileId"
                  {...qualityProfileId}
                  onChange={handleInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('Tags')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.TAG}
                  name="tags"
                  {...tags}
                  onChange={handleInputChange}
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
            {...searchForGame}
            onChange={handleInputChange}
          />
        </label>

        <SpinnerButton
          className={styles.addButton}
          kind={kinds.SUCCESS}
          isSpinning={isAdding}
          onPress={handleAddGamePress}
        >
          {translate('AddGame')}
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default AddNewGameCollectionGameModalContent;
