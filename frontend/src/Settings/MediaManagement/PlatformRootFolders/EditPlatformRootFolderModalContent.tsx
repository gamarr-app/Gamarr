import { useCallback, useEffect, useMemo, useRef } from 'react';
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
import platformOptions from 'Game/platformOptions';
import { inputTypes, kinds } from 'Helpers/Props';
import {
  PlatformRootFolderItem,
  savePlatformRootFolder,
  setPlatformRootFolderValue,
} from 'Store/Actions/settingsActions';
import selectSettings from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './EditPlatformRootFolderModalContent.css';

const newPlatformRootFolder: Record<string, string> = {
  platform: 'unknown',
  path: '',
};

function createPlatformRootFolderSelector(id: number | undefined) {
  return createSelector(
    (state: {
      settings: {
        platformRootFolders: {
          isFetching: boolean;
          error: AppError | undefined;
          isSaving: boolean;
          saveError: AppError | undefined;
          pendingChanges: Partial<PlatformRootFolderItem>;
          items: PlatformRootFolderItem[];
        };
      };
    }) => state.settings.platformRootFolders,
    (platformRootFolders) => {
      const { isFetching, error, isSaving, saveError, pendingChanges, items } =
        platformRootFolders;

      const platformRootFolder = id
        ? (items.find((i) => i.id === id) ?? newPlatformRootFolder)
        : newPlatformRootFolder;

      const settings = selectSettings(
        platformRootFolder,
        pendingChanges,
        saveError
      );

      // One default per platform, so anything already configured elsewhere is
      // off the menu; the row being edited keeps its own platform.
      const takenPlatforms = items
        .filter((i) => i.id !== id)
        .map((i) => i.platform);

      return {
        id,
        isFetching,
        error,
        isSaving,
        saveError,
        item: settings.settings,
        ...settings,
        availablePlatforms: platformOptions.filter(
          (p) => !takenPlatforms.includes(p.key)
        ),
      };
    }
  );
}

interface EditPlatformRootFolderModalContentProps {
  id?: number;
  onModalClose: () => void;
  onDeletePlatformRootFolderPress?: () => void;
}

function EditPlatformRootFolderModalContent({
  id,
  onModalClose,
  onDeletePlatformRootFolderPress,
}: EditPlatformRootFolderModalContentProps) {
  const dispatch = useDispatch();

  const {
    isFetching,
    error,
    isSaving,
    saveError,
    item,
    availablePlatforms,
    id: _id,
    ...otherSettings
  } = useSelector(useMemo(() => createPlatformRootFolderSelector(id), [id]));

  const prevIsSaving = useRef(isSaving);

  useEffect(() => {
    if (!id) {
      Object.keys(newPlatformRootFolder).forEach((name) => {
        dispatch(
          setPlatformRootFolderValue({
            name,
            value: newPlatformRootFolder[name],
          })
        );
      });
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
      dispatch(setPlatformRootFolderValue({ name, value }));
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(savePlatformRootFolder({ id }));
  }, [dispatch, id]);

  const { platform, path } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id
          ? translate('EditPlatformRootFolder')
          : translate('AddPlatformRootFolder')}
      </ModalHeader>

      <ModalBody className={styles.body}>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>
            {translate('PlatformRootFoldersLoadError')}
          </Alert>
        ) : null}

        {!isFetching && !error ? (
          <Form {...otherSettings}>
            <FormGroup>
              <FormLabel>{translate('Platform')}</FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="platform"
                helpText={translate('PlatformRootFolderPlatformHelpText')}
                {...platform}
                values={availablePlatforms}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('RootFolder')}</FormLabel>

              <FormInputGroup
                type={inputTypes.ROOT_FOLDER_SELECT}
                name="path"
                helpText={translate('PlatformRootFolderPathHelpText')}
                {...path}
                onChange={handleInputChange}
              />
            </FormGroup>
          </Form>
        ) : null}
      </ModalBody>

      <ModalFooter>
        {id ? (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeletePlatformRootFolderPress}
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

export default EditPlatformRootFolderModalContent;
