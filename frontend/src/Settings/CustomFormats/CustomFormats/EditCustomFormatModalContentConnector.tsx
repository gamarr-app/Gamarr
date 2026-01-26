import { useCallback, useEffect, useMemo, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import {
  cloneCustomFormatSpecification,
  deleteCustomFormatSpecification,
  fetchCustomFormatSpecifications,
  saveCustomFormat,
  setCustomFormatValue,
} from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import CustomFormat from 'typings/CustomFormat';
import { InputChanged } from 'typings/inputs';
import EditCustomFormatModalContent from './EditCustomFormatModalContent';

interface EditCustomFormatModalContentConnectorProps {
  id?: number;
  tagsFromId?: number;
  onContentHeightChange: (height: number) => void;
  onModalClose: () => void;
  onDeleteCustomFormatPress?: () => void;
}

function EditCustomFormatModalContentConnector({
  id,
  tagsFromId,
  onContentHeightChange,
  onModalClose,
  onDeleteCustomFormatPress,
}: EditCustomFormatModalContentConnectorProps) {
  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelectorHook<CustomFormat>('customFormats', id),
    [id]
  );

  const advancedSettings = useSelector(
    (state: { settings: { advancedSettings: boolean } }) =>
      state.settings.advancedSettings
  );

  const customFormat = useSelector(providerSettingsSelector);

  const specificationsSelector = useMemo(
    () =>
      createSelector(
        (state: {
          settings: {
            customFormatSpecifications: {
              isPopulated: boolean;
              items: Array<{
                id: number;
                name: string;
                implementation: string;
                implementationName: string;
                negate: boolean;
                required: boolean;
                fields: object[];
              }>;
            };
          };
        }) => state.settings.customFormatSpecifications,
        (specifications) => ({
          specificationsPopulated: specifications.isPopulated,
          specifications: specifications.items,
        })
      ),
    []
  );

  const { specificationsPopulated, specifications } = useSelector(
    specificationsSelector
  );

  const prevIsSaving = useRef(customFormat.isSaving);

  useEffect(() => {
    dispatch(fetchCustomFormatSpecifications({ id: tagsFromId || id }));
  }, [dispatch, id, tagsFromId]);

  useEffect(() => {
    if (
      prevIsSaving.current &&
      !customFormat.isSaving &&
      !customFormat.saveError
    ) {
      onModalClose();
    }
    prevIsSaving.current = customFormat.isSaving;
  }, [customFormat.isSaving, customFormat.saveError, onModalClose]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setCustomFormatValue({ name, value }));
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveCustomFormat({ id }));
  }, [dispatch, id]);

  const handleCloneSpecificationPress = useCallback(
    (specId: number) => {
      dispatch(cloneCustomFormatSpecification({ id: specId }));
    },
    [dispatch]
  );

  const handleConfirmDeleteSpecification = useCallback(
    (specId: number) => {
      dispatch(deleteCustomFormatSpecification({ id: specId }));
    },
    [dispatch]
  );

  return (
    <EditCustomFormatModalContent
      advancedSettings={advancedSettings}
      {...customFormat}
      id={id}
      specificationsPopulated={specificationsPopulated}
      specifications={specifications}
      onContentHeightChange={onContentHeightChange}
      onModalClose={onModalClose}
      onDeleteCustomFormatPress={onDeleteCustomFormatPress}
      onSavePress={handleSavePress}
      onInputChange={handleInputChange}
      onCloneSpecificationPress={handleCloneSpecificationPress}
      onConfirmDeleteSpecification={handleConfirmDeleteSpecification}
    />
  );
}

export default EditCustomFormatModalContentConnector;
