import React, { useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchCustomFormatSpecifications } from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import ExportCustomFormatModalContent from './ExportCustomFormatModalContent';

const omittedProperties = ['id', 'implementationName', 'infoLink'];

function replacer(key: string, value: any) {
  if (omittedProperties.includes(key)) {
    return undefined;
  }

  if (key === 'fields') {
    return value.reduce(
      (acc: Record<string, any>, cur: { name: string; value: any }) => {
        acc[cur.name] = cur.value;
        return acc;
      },
      {}
    );
  }

  if (value && typeof value === 'object' && 'value' in value) {
    return value.value;
  }

  return value;
}

interface ExportCustomFormatModalContentConnectorProps {
  id?: number;
  onModalClose: () => void;
}

function ExportCustomFormatModalContentConnector({
  id,
  onModalClose,
}: ExportCustomFormatModalContentConnectorProps) {
  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelectorHook('customFormats', id),
    [id]
  );

  const advancedSettings = useSelector(
    (state: { settings: { advancedSettings: boolean } }) =>
      state.settings.advancedSettings
  );

  const customFormat = useSelector(providerSettingsSelector) as any;

  const specificationsSelector = useMemo(
    () =>
      createSelector(
        (state: {
          settings: {
            customFormatSpecifications: {
              isPopulated: boolean;
              items: any[];
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

  const json = customFormat.item
    ? JSON.stringify(customFormat.item, replacer, 2)
    : '';

  useEffect(() => {
    dispatch(fetchCustomFormatSpecifications({ id }));
  }, [dispatch, id]);

  return (
    <ExportCustomFormatModalContent
      advancedSettings={advancedSettings}
      {...customFormat}
      id={id}
      json={json}
      specificationsPopulated={specificationsPopulated}
      specifications={specifications}
      onModalClose={onModalClose}
    />
  );
}

export default ExportCustomFormatModalContentConnector;
