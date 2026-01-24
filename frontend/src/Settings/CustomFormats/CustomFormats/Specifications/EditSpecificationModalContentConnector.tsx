import React, { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  clearCustomFormatSpecificationPending,
  saveCustomFormatSpecification,
  setCustomFormatSpecificationFieldValue,
  setCustomFormatSpecificationValue,
} from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import { InputChanged } from 'typings/inputs';
import EditSpecificationModalContent from './EditSpecificationModalContent';

interface EditSpecificationModalContentConnectorProps {
  id?: number;
  onModalClose: () => void;
}

function EditSpecificationModalContentConnector({
  id,
  onModalClose,
}: EditSpecificationModalContentConnectorProps) {
  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelectorHook('customFormatSpecifications', id),
    [id]
  );

  const advancedSettings = useSelector(
    (state: { settings: { advancedSettings: boolean } }) =>
      state.settings.advancedSettings
  );

  const specification = useSelector(providerSettingsSelector) as any;

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setCustomFormatSpecificationValue({ name, value }));
    },
    [dispatch]
  );

  const handleFieldChange = useCallback(
    ({ name, value }: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setCustomFormatSpecificationFieldValue({ name, value }));
    },
    [dispatch]
  );

  const handleCancelPress = useCallback(() => {
    dispatch(clearCustomFormatSpecificationPending());
    onModalClose();
  }, [dispatch, onModalClose]);

  const handleSavePress = useCallback(() => {
    dispatch(saveCustomFormatSpecification({ id }));
    onModalClose();
  }, [dispatch, id, onModalClose]);

  return (
    <EditSpecificationModalContent
      advancedSettings={advancedSettings}
      {...specification}
      id={id}
      onCancelPress={handleCancelPress}
      onSavePress={handleSavePress}
      onInputChange={handleInputChange}
      onFieldChange={handleFieldChange}
      onModalClose={onModalClose}
    />
  );
}

export default EditSpecificationModalContentConnector;
