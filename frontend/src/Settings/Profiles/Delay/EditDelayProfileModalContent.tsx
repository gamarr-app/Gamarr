import { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { Error as AppError } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import {
  saveDelayProfile,
  setDelayProfileValue,
} from 'Store/Actions/settingsActions';
import selectSettings from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './EditDelayProfileModalContent.css';

const protocolOptions = [
  {
    key: 'preferUsenet',
    get value() {
      return translate('PreferUsenet');
    },
  },
  {
    key: 'preferTorrent',
    get value() {
      return translate('PreferTorrent');
    },
  },
  {
    key: 'onlyUsenet',
    get value() {
      return translate('OnlyUsenet');
    },
  },
  {
    key: 'onlyTorrent',
    get value() {
      return translate('OnlyTorrent');
    },
  },
];

interface DelayProfileModel {
  enableUsenet: boolean;
  enableTorrent: boolean;
  preferredProtocol: string;
  usenetDelay: number;
  torrentDelay: number;
  bypassIfHighestQuality: boolean;
  bypassIfAboveCustomFormatScore: boolean;
  minimumCustomFormatScore: number;
  tags: number[];
}

const newDelayProfile: DelayProfileModel = {
  enableUsenet: true,
  enableTorrent: true,
  preferredProtocol: 'usenet',
  usenetDelay: 0,
  torrentDelay: 0,
  bypassIfHighestQuality: false,
  bypassIfAboveCustomFormatScore: false,
  minimumCustomFormatScore: 0,
  tags: [],
};

function createDelayProfileSelector(id: number | undefined) {
  return createSelector(
    (state: {
      settings: {
        delayProfiles: {
          isFetching: boolean;
          error: AppError | undefined;
          isSaving: boolean;
          saveError: AppError | undefined;
          pendingChanges: Record<string, unknown>;
          items: Array<{ id: number } & Partial<DelayProfileModel>>;
        };
      };
    }) => state.settings.delayProfiles,
    (delayProfiles) => {
      const { isFetching, error, isSaving, saveError, pendingChanges, items } =
        delayProfiles;

      const profile = id
        ? (items.find((i) => i.id === id) as DelayProfileModel | undefined)
        : newDelayProfile;
      const settings = selectSettings(
        profile ?? newDelayProfile,
        pendingChanges,
        saveError
      );

      const enableUsenet = settings.settings.enableUsenet.value;
      const enableTorrent = settings.settings.enableTorrent.value;
      const preferredProtocol = settings.settings.preferredProtocol.value;
      let protocol = 'preferUsenet';

      if (preferredProtocol === 'usenet') {
        protocol = 'preferUsenet';
      } else {
        protocol = 'preferTorrent';
      }

      if (!enableUsenet) {
        protocol = 'onlyTorrent';
      }

      if (!enableTorrent) {
        protocol = 'onlyUsenet';
      }

      return {
        id,
        isFetching,
        error,
        isSaving,
        saveError,
        protocol,
        item: settings.settings,
        ...settings,
      };
    }
  );
}

interface EditDelayProfileModalContentProps {
  id?: number;
  onModalClose: () => void;
  onDeleteDelayProfilePress?: () => void;
}

function EditDelayProfileModalContent({
  id,
  onModalClose,
  onDeleteDelayProfilePress,
}: EditDelayProfileModalContentProps) {
  const dispatch = useDispatch();

  const {
    isFetching,
    error,
    isSaving,
    saveError,
    protocol,
    item,
    validationErrors,
    validationWarnings,
  } = useSelector(createDelayProfileSelector(id));

  const prevIsSaving = useRef(isSaving);

  useEffect(() => {
    if (!id) {
      (Object.keys(newDelayProfile) as Array<keyof DelayProfileModel>).forEach(
        (name) => {
          dispatch(
            setDelayProfileValue({
              name,
              value: newDelayProfile[name],
            })
          );
        }
      );
    }
  }, [dispatch, id]);

  useEffect(() => {
    if (prevIsSaving.current && !isSaving && !saveError) {
      onModalClose();
    }
    prevIsSaving.current = isSaving;
  }, [isSaving, saveError, onModalClose]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setDelayProfileValue({ name, value }));
    },
    [dispatch]
  );

  const handleProtocolChange = useCallback(
    ({ value }: InputChanged<string>) => {
      switch (value) {
        case 'preferUsenet':
          dispatch(setDelayProfileValue({ name: 'enableUsenet', value: true }));
          dispatch(
            setDelayProfileValue({ name: 'enableTorrent', value: true })
          );
          dispatch(
            setDelayProfileValue({ name: 'preferredProtocol', value: 'usenet' })
          );
          break;
        case 'preferTorrent':
          dispatch(setDelayProfileValue({ name: 'enableUsenet', value: true }));
          dispatch(
            setDelayProfileValue({ name: 'enableTorrent', value: true })
          );
          dispatch(
            setDelayProfileValue({
              name: 'preferredProtocol',
              value: 'torrent',
            })
          );
          break;
        case 'onlyUsenet':
          dispatch(setDelayProfileValue({ name: 'enableUsenet', value: true }));
          dispatch(
            setDelayProfileValue({ name: 'enableTorrent', value: false })
          );
          dispatch(
            setDelayProfileValue({ name: 'preferredProtocol', value: 'usenet' })
          );
          break;
        case 'onlyTorrent':
          dispatch(
            setDelayProfileValue({ name: 'enableUsenet', value: false })
          );
          dispatch(
            setDelayProfileValue({ name: 'enableTorrent', value: true })
          );
          dispatch(
            setDelayProfileValue({
              name: 'preferredProtocol',
              value: 'torrent',
            })
          );
          break;
        default:
          throw Error(`Unknown protocol option: ${value}`);
      }
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveDelayProfile({ id }));
  }, [dispatch, id]);

  const {
    enableUsenet,
    enableTorrent,
    usenetDelay,
    torrentDelay,
    bypassIfHighestQuality,
    bypassIfAboveCustomFormatScore,
    minimumCustomFormatScore,
    tags,
  } = item;

  // saveError is already the correct type for SpinnerErrorButton

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? translate('EditDelayProfile') : translate('AddDelayProfile')}
      </ModalHeader>

      <ModalBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>{translate('AddDelayProfileError')}</Alert>
        ) : null}

        {!isFetching && !error ? (
          <Form
            validationErrors={validationErrors}
            validationWarnings={validationWarnings}
          >
            <FormGroup>
              <FormLabel>{translate('PreferredProtocol')}</FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="protocol"
                value={protocol}
                values={protocolOptions}
                helpText={translate('ProtocolHelpText')}
                onChange={handleProtocolChange}
              />
            </FormGroup>

            {enableUsenet.value ? (
              <FormGroup>
                <FormLabel>{translate('UsenetDelay')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="usenetDelay"
                  unit="minutes"
                  {...usenetDelay}
                  helpText={translate('UsenetDelayHelpText')}
                  onChange={handleInputChange}
                />
              </FormGroup>
            ) : null}

            {enableTorrent.value ? (
              <FormGroup>
                <FormLabel>{translate('TorrentDelay')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="torrentDelay"
                  unit="minutes"
                  {...torrentDelay}
                  helpText={translate('TorrentDelayHelpText')}
                  onChange={handleInputChange}
                />
              </FormGroup>
            ) : null}

            <FormGroup>
              <FormLabel>{translate('BypassDelayIfHighestQuality')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="bypassIfHighestQuality"
                {...bypassIfHighestQuality}
                helpText={translate('BypassDelayIfHighestQualityHelpText')}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('BypassDelayIfAboveCustomFormatScore')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="bypassIfAboveCustomFormatScore"
                {...bypassIfAboveCustomFormatScore}
                helpText={translate(
                  'BypassDelayIfAboveCustomFormatScoreHelpText'
                )}
                onChange={handleInputChange}
              />
            </FormGroup>

            {bypassIfAboveCustomFormatScore.value ? (
              <FormGroup>
                <FormLabel>
                  {translate('BypassDelayIfAboveCustomFormatScoreMinimumScore')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="minimumCustomFormatScore"
                  {...minimumCustomFormatScore}
                  helpText={translate(
                    'BypassDelayIfAboveCustomFormatScoreMinimumScoreHelpText'
                  )}
                  onChange={handleInputChange}
                />
              </FormGroup>
            ) : null}

            {id === 1 ? (
              <Alert>{translate('DefaultDelayProfileGame')}</Alert>
            ) : (
              <FormGroup>
                <FormLabel>{translate('Tags')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.TAG}
                  name="tags"
                  {...tags}
                  helpText={translate('DelayProfileGameTagsHelpText')}
                  onChange={handleInputChange}
                />
              </FormGroup>
            )}
          </Form>
        ) : null}
      </ModalBody>
      <ModalFooter>
        {id && id > 1 ? (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteDelayProfilePress}
          >
            {translate('Delete')}
          </Button>
        ) : null}

        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={handleSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditDelayProfileModalContent;
