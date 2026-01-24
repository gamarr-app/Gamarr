import _ from 'lodash';
import React, { useCallback, useEffect, useMemo } from 'react';
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
              schema: any[];
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
    (fields: Record<string, any>, schema: any) => {
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
        // @ts-expect-error - actions aren't typed
        dispatch(setCustomFormatSpecificationFieldValue({ name: key, value }));
      }
    },
    [dispatch]
  );

  const parseSpecification = useCallback(
    (spec: any) => {
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
          parseFields(value as Record<string, any>, selectedImplementation);
        } else if (key !== 'id') {
          // @ts-expect-error - actions aren't typed
          dispatch(setCustomFormatSpecificationValue({ name: key, value }));
        }
      }

      dispatch(saveCustomFormatSpecification());
    },
    [dispatch, specificationSchema, parseFields]
  );

  const parseCf = useCallback(
    (cf: any) => {
      for (const [key, value] of Object.entries(cf)) {
        if (key === 'specifications') {
          for (const spec of value as any[]) {
            parseSpecification(spec);
          }
        } else if (key !== 'id') {
          // @ts-expect-error - actions aren't typed
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
      } catch (err: any) {
        clearPending();
        return {
          message: err.message,
          detailedMessage: err.stack,
        };
      }

      return null;
    },
    [clearPending, parseCf]
  );

  return (
    <ImportCustomFormatModalContent
      advancedSettings={advancedSettings}
      {...customFormat}
      id={id}
      specificationsPopulated={specificationsPopulated}
      specificationSchema={specificationSchema}
      onImportPress={handleImportPress}
      onModalClose={onModalClose}
    />
  );
}

export default ImportCustomFormatModalContentConnector;
