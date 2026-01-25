import _ from 'lodash';
import { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import {
  clearCustomFormatSpecificationPending,
  deleteAllCustomFormatSpecification,
  fetchCustomFormatSpecificationSchema,
  saveCustomFormatSpecification,
  selectCustomFormatSpecificationSchema,
  setCustomFormatSpecificationFieldValue,
  setCustomFormatSpecificationValue,
  setCustomFormatValue,
} from 'Store/Actions/settingsActions';
import { createProviderSettingsSelectorHook } from 'Store/Selectors/createProviderSettingsSelector';
import translate from 'Utilities/String/translate';
import ImportCustomFormatModalContent from './ImportCustomFormatModalContent';

interface ImportCustomFormatModalContentConnectorProps {
  id?: number;
  onContentHeightChange?: (height: number) => void;
  onModalClose: () => void;
}

function ImportCustomFormatModalContentConnector({
  id,
  onModalClose,
}: ImportCustomFormatModalContentConnectorProps) {
  const dispatch = useDispatch();

  const providerSettingsSelector = useMemo(
    () => createProviderSettingsSelectorHook('customFormats', id),
    [id]
  );

  const customFormat = useSelector(providerSettingsSelector) as unknown as {
    isFetching: boolean;
    error: object | null;
    isSaving: boolean;
    saveError: object | null;
    item: Record<string, unknown>;
  };

  const specificationsSelector = useMemo(
    () =>
      createSelector(
        (state: {
          settings: {
            customFormatSpecifications: {
              isPopulated: boolean;
              schema: Array<{
                implementation: string;
                implementationName: string;
                fields: Array<{ name: string }>;
                [key: string]: unknown;
              }>;
            };
          };
        }) => state.settings.customFormatSpecifications,
        (specifications) => ({
          specificationsPopulated: specifications.isPopulated,
          specificationSchema: specifications.schema,
        })
      ),
    []
  );

  const { specificationsPopulated, specificationSchema } = useSelector(
    specificationsSelector
  );

  useEffect(() => {
    dispatch(fetchCustomFormatSpecificationSchema());
  }, [dispatch]);

  const clearPending = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.customFormats' }));
    dispatch(clearCustomFormatSpecificationPending());
    dispatch(deleteAllCustomFormatSpecification());
  }, [dispatch]);

  const parseFields = useCallback(
    (
      fields: Record<string, unknown>,
      schema: { implementationName: string; fields: Array<{ name: string }> }
    ) => {
      for (const [key, value] of Object.entries(fields)) {
        const field = _.find(schema.fields, { name: key });
        if (!field) {
          throw new Error(
            translate('CustomFormatUnknownConditionOption', {
              key,
              implementation: schema.implementationName,
            })
          );
        }
        dispatch(setCustomFormatSpecificationFieldValue({ name: key, value }));
      }
    },
    [dispatch]
  );

  const parseSpecification = useCallback(
    (spec: {
      implementation: string;
      fields?: Record<string, unknown>;
      [key: string]: unknown;
    }) => {
      const selectedImplementation = _.find(specificationSchema, {
        implementation: spec.implementation,
      });

      if (!selectedImplementation) {
        throw new Error(
          translate('CustomFormatUnknownCondition', {
            implementation: spec.implementation,
          })
        );
      }

      dispatch(
        selectCustomFormatSpecificationSchema({
          implementation: spec.implementation,
        })
      );

      for (const [key, value] of Object.entries(spec)) {
        if (key === 'fields') {
          parseFields(value as Record<string, unknown>, selectedImplementation);
        } else if (key !== 'id') {
          dispatch(setCustomFormatSpecificationValue({ name: key, value }));
        }
      }

      dispatch(saveCustomFormatSpecification());
    },
    [dispatch, specificationSchema, parseFields]
  );

  const parseCf = useCallback(
    (cf: Record<string, unknown>) => {
      for (const [key, value] of Object.entries(cf)) {
        if (key === 'specifications') {
          for (const spec of value as Array<{
            implementation: string;
            [key: string]: unknown;
          }>) {
            parseSpecification(spec);
          }
        } else if (key !== 'id') {
          dispatch(setCustomFormatValue({ name: key, value }));
        }
      }
    },
    [dispatch, parseSpecification]
  );

  const handleImportPress = useCallback(
    (payload: string) => {
      clearPending();

      try {
        const cf = JSON.parse(payload);
        parseCf(cf);
      } catch (err: unknown) {
        clearPending();
        const error = err as Error;
        return {
          message: error.message,
          detailedMessage: error.stack,
        };
      }

      return null;
    },
    [clearPending, parseCf]
  );

  return (
    <ImportCustomFormatModalContent
      isFetching={customFormat.isFetching}
      error={customFormat.error as Error | undefined}
      specificationsPopulated={specificationsPopulated}
      onImportPress={handleImportPress}
      onModalClose={onModalClose}
    />
  );
}

export default ImportCustomFormatModalContentConnector;
