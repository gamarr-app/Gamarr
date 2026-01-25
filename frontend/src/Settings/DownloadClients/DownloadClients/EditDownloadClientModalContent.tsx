import React, { useCallback, useEffect, useMemo, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import ProviderFieldFormGroup from 'Components/Form/ProviderFieldFormGroup';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import AdvancedSettingsButton from 'Settings/AdvancedSettingsButton';
import {
  saveDownloadClient,
  setDownloadClientFieldValue,
  setDownloadClientValue,
  testDownloadClient,
} from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import DownloadClient from 'typings/DownloadClient';
import { InputChanged } from 'typings/inputs';
import { PendingSection } from 'typings/pending';
import translate from 'Utilities/String/translate';
import styles from './EditDownloadClientModalContent.css';

interface EditDownloadClientModalContentProps {
  id?: number;
  onModalClose: () => void;
  onDeleteDownloadClientPress?: () => void;
}

function EditDownloadClientModalContent({
  id,
  onModalClose,
  onDeleteDownloadClientPress,
}: EditDownloadClientModalContentProps) {
  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelectorHook('downloadClients', id),
    [id]
  );

  const advancedSettings = useSelector(
    (state: { settings: { advancedSettings: boolean } }) =>
      state.settings.advancedSettings
  );

  const {
    isFetching,
    error,
    isSaving,
    isTesting,
    saveError,
    item,
    ...otherSettings
  } = useSelector(providerSettingsSelector);

  const typedItem = item as PendingSection<DownloadClient>;

  const prevIsSaving = useRef(isSaving);

  useEffect(() => {
    if (prevIsSaving.current && !isSaving && !saveError) {
      onModalClose();
    }
    prevIsSaving.current = isSaving;
  }, [isSaving, saveError, onModalClose]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setDownloadClientValue({ name, value }));
    },
    [dispatch]
  );

  const handleFieldChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setDownloadClientFieldValue({ name, value }));
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveDownloadClient({ id }));
  }, [dispatch, id]);

  const handleTestPress = useCallback(() => {
    dispatch(testDownloadClient({ id }));
  }, [dispatch, id]);

  const {
    implementationName = '',
    name,
    enable,
    priority,
    removeCompletedDownloads,
    removeFailedDownloads,
    fields,
    tags,
    message,
  } = typedItem;

  // saveError is already the correct type for SpinnerErrorButton

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id
          ? translate('EditDownloadClientImplementation', {
              implementationName,
            })
          : translate('AddDownloadClientImplementation', {
              implementationName,
            })}
      </ModalHeader>

      <ModalBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>
            {translate('AddDownloadClientError')}
          </Alert>
        ) : null}

        {!isFetching && !error ? (
          <Form {...otherSettings}>
            {!!message?.value && (
              <Alert className={styles.message} kind={message.value.type}>
                {message.value.message}
              </Alert>
            )}

            <FormGroup>
              <FormLabel>{translate('Name')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TEXT}
                name="name"
                {...name}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('Enable')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="enable"
                {...enable}
                onChange={handleInputChange}
              />
            </FormGroup>

            {(fields ?? []).map((field) => {
              return (
                <ProviderFieldFormGroup
                  key={field.name}
                  advancedSettings={advancedSettings}
                  provider="downloadClient"
                  providerData={typedItem}
                  {...field}
                  onChange={handleFieldChange}
                />
              );
            })}

            <FormGroup advancedSettings={advancedSettings} isAdvanced={true}>
              <FormLabel>{translate('ClientPriority')}</FormLabel>

              <FormInputGroup
                type={inputTypes.NUMBER}
                name="priority"
                helpText={translate('DownloadClientPriorityHelpText')}
                min={1}
                max={50}
                {...priority}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('Tags')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TAG}
                name="tags"
                helpText={translate('DownloadClientGameTagHelpText')}
                {...tags}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FieldSet
              size={sizes.SMALL}
              legend={translate('CompletedDownloadHandling')}
            >
              <FormGroup>
                <FormLabel>{translate('RemoveCompleted')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="removeCompletedDownloads"
                  helpText={translate('RemoveCompletedDownloadsHelpText')}
                  {...removeCompletedDownloads}
                  onChange={handleInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('RemoveFailed')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="removeFailedDownloads"
                  helpText={translate('RemoveFailedDownloadsHelpText')}
                  {...removeFailedDownloads}
                  onChange={handleInputChange}
                />
              </FormGroup>
            </FieldSet>
          </Form>
        ) : null}
      </ModalBody>
      <ModalFooter>
        {id ? (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteDownloadClientPress}
          >
            {translate('Delete')}
          </Button>
        ) : null}

        <AdvancedSettingsButton showLabel={false} />

        <SpinnerErrorButton
          isSpinning={isTesting ?? false}
          error={saveError}
          onPress={handleTestPress}
        >
          {translate('Test')}
        </SpinnerErrorButton>

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

export default EditDownloadClientModalContent;
