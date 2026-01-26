import { useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchCustomFormatSpecifications } from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import CustomFormat from 'typings/CustomFormat';
import ExportCustomFormatModalContent from './ExportCustomFormatModalContent';

const omittedProperties = ['id', 'implementationName', 'infoLink'];

function replacer(key: string, value: unknown): unknown {
  if (omittedProperties.includes(key)) {
    return undefined;
  }

  if (key === 'fields') {
    return (value as Array<{ name: string; value: unknown }>).reduce(
      (acc: Record<string, unknown>, cur: { name: string; value: unknown }) => {
        acc[cur.name] = cur.value;
        return acc;
      },
      {}
    );
  }

  if (value && typeof value === 'object' && 'value' in value) {
    return (value as { value: unknown }).value;
  }

  return value;
}

interface ExportCustomFormatModalContentConnectorProps {
  id?: number;
  onContentHeightChange?: (height: number) => void;
  onModalClose: () => void;
}

function ExportCustomFormatModalContentConnector({
  id,
  onModalClose,
}: ExportCustomFormatModalContentConnectorProps) {
  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelectorHook<CustomFormat>('customFormats', id),
    [id]
  );

  const customFormat = useSelector(providerSettingsSelector);

  const specificationsSelector = useMemo(
    () =>
      createSelector(
        (state: {
          settings: {
            customFormatSpecifications: {
              isPopulated: boolean;
              items: Array<{ id: number; [key: string]: unknown }>;
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

  const { specificationsPopulated } = useSelector(specificationsSelector);

  const json = customFormat.item
    ? JSON.stringify(customFormat.item, replacer, 2)
    : '';

  useEffect(() => {
    dispatch(fetchCustomFormatSpecifications({ id }));
  }, [dispatch, id]);

  return (
    <ExportCustomFormatModalContent
      isFetching={customFormat.isFetching}
      error={customFormat.error ?? undefined}
      json={json}
      specificationsPopulated={specificationsPopulated}
      onModalClose={onModalClose}
    />
  );
}

export default ExportCustomFormatModalContentConnector;
