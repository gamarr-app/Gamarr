import React, { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Alert from 'Components/Alert';
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
import useShowAdvancedSettings from 'Helpers/Hooks/useShowAdvancedSettings';
import { inputTypes, kinds } from 'Helpers/Props';
import AdvancedSettingsButton from 'Settings/AdvancedSettingsButton';
import {
  saveNotification,
  setNotificationFieldValues,
  setNotificationValue,
  testNotification,
} from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import { InputChanged } from 'typings/inputs';
import NotificationType from 'typings/Notification';
import { PendingSection } from 'typings/pending';
import translate from 'Utilities/String/translate';
import NotificationEventItems from './NotificationEventItems';
import styles from './EditNotificationModalContent.css';

interface EditNotificationModalContentProps {
  id?: number;
  onModalClose: () => void;
  onDeleteNotificationPress?: () => void;
}

function EditNotificationModalContent({
  id,
  onModalClose,
  onDeleteNotificationPress,
}: EditNotificationModalContentProps) {
  const dispatch = useDispatch();
  const advancedSettings = useShowAdvancedSettings();

  const {
    isFetching,
    error,
    isSaving,
    isTesting,
    saveError,
    item,
    ...otherSettings
  } = useSelector(createProviderSettingsSelectorHook('notifications', id));

  const prevIsSaving = useRef(isSaving);

  useEffect(() => {
    if (prevIsSaving.current && !isSaving && !saveError) {
      onModalClose();
    }
    prevIsSaving.current = isSaving;
  }, [isSaving, saveError, onModalClose]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setNotificationValue({ name, value }));
    },
    [dispatch]
  );

  const handleFieldChange = useCallback(
    ({
      name,
      value,
      additionalProperties = {},
    }: InputChanged & { additionalProperties?: Record<string, unknown> }) => {
      dispatch(
        // @ts-expect-error - actions aren't typed
        setNotificationFieldValues({
          properties: { ...additionalProperties, [name]: value },
        })
      );
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveNotification({ id }));
  }, [dispatch, id]);

  const handleTestPress = useCallback(() => {
    dispatch(testNotification({ id }));
  }, [dispatch, id]);

  const typedItem = item as PendingSection<NotificationType>;
  const { implementationName = '', name, tags, fields, message } = typedItem;

  // saveError is already the correct type for SpinnerErrorButton

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id
          ? translate('EditConnectionImplementation', { implementationName })
          : translate('AddConnectionImplementation', { implementationName })}
      </ModalHeader>

      <ModalBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>{translate('AddNotificationError')}</Alert>
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

            <NotificationEventItems
              item={typedItem}
              onInputChange={handleInputChange}
            />

            <FormGroup>
              <FormLabel>{translate('Tags')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TAG}
                name="tags"
                helpText={translate('NotificationsTagsGameHelpText')}
                {...tags}
                onChange={handleInputChange}
              />
            </FormGroup>

            {(fields ?? []).map((field) => {
              return (
                <ProviderFieldFormGroup
                  key={field.name}
                  advancedSettings={advancedSettings}
                  provider="notification"
                  providerData={typedItem}
                  {...field}
                  section="settings.notifications"
                  onChange={handleFieldChange}
                />
              );
            })}
          </Form>
        ) : null}
      </ModalBody>
      <ModalFooter>
        {id ? (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteNotificationPress}
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

export default EditNotificationModalContent;
