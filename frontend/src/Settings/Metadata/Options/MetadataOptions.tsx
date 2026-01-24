import React, { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import {
  fetchMetadataOptions,
  saveMetadataOptions,
  setMetadataOptionsValue,
} from 'Store/Actions/settingsActions';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import { InputChanged } from 'typings/inputs';
import {
  OnChildStateChange,
  SetChildSave,
} from 'typings/Settings/SettingsState';
import translate from 'Utilities/String/translate';

const SECTION = 'metadataOptions';

export const certificationCountryOptions = [
  { key: 'us', value: 'United States' },
  { key: 'au', value: 'Australia' },
  { key: 'br', value: 'Brazil' },
  { key: 'ca', value: 'Canada' },
  { key: 'fr', value: 'France' },
  { key: 'de', value: 'Germany' },
  { key: 'gb', value: 'Great Britain' },
  { key: 'in', value: 'India' },
  { key: 'ie', value: 'Ireland' },
  { key: 'it', value: 'Italy' },
  { key: 'nz', value: 'New Zealand' },
  { key: 'ro', value: 'Romania' },
  { key: 'es', value: 'Spain' },
];

interface MetadataOptionsProps {
  setChildSave: SetChildSave;
  onChildStateChange: OnChildStateChange;
}

function MetadataOptions({
  setChildSave,
  onChildStateChange,
}: MetadataOptionsProps) {
  const dispatch = useDispatch();
  const {
    isFetching,
    isPopulated,
    isSaving,
    error,
    settings,
    hasSettings,
    hasPendingChanges,
  } = useSelector(createSettingsSectionSelector(SECTION));

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setMetadataOptionsValue(change));
    },
    [dispatch]
  );

  useEffect(() => {
    dispatch(fetchMetadataOptions());
    setChildSave(() => dispatch(saveMetadataOptions()));
  }, [dispatch, setChildSave]);

  useEffect(() => {
    onChildStateChange({
      isSaving,
      hasPendingChanges,
    });
  }, [hasPendingChanges, isSaving, onChildStateChange]);

  useEffect(() => {
    return () => {
      dispatch(clearPendingChanges({ section: `settings.${SECTION}` }));
    };
  }, [dispatch]);

  return (
    <FieldSet legend={translate('Options')}>
      {isFetching ? <LoadingIndicator /> : null}

      {!isFetching && error ? (
        <Alert kind={kinds.DANGER}>
          {translate('UnableToLoadIndexerOptions')}
        </Alert>
      ) : null}

      {hasSettings && isPopulated && !error ? (
        <Form>
          <FormGroup>
            <FormLabel>{translate('CertificationCountry')}</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="certificationCountry"
              values={certificationCountryOptions}
              onChange={handleInputChange}
              helpText={translate('CertificationCountryHelpText')}
              {...settings.certificationCountry}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>IGDB Client ID</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="igdbClientId"
              onChange={handleInputChange}
              helpText="Client ID from IGDB/Twitch Developer Portal. Enables better cover art for DLC and expansions."
              helpTextWarning="Get credentials at https://api-docs.igdb.com/"
              {...settings.igdbClientId}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>IGDB Client Secret</FormLabel>

            <FormInputGroup
              type={inputTypes.PASSWORD}
              name="igdbClientSecret"
              onChange={handleInputChange}
              helpText="Client Secret from IGDB/Twitch Developer Portal"
              {...settings.igdbClientSecret}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>RAWG API Key</FormLabel>

            <FormInputGroup
              type={inputTypes.PASSWORD}
              name="rawgApiKey"
              onChange={handleInputChange}
              helpText="API key from RAWG.io. Provides additional game metadata and cover art."
              helpTextWarning="Get a free key at https://rawg.io/apidocs"
              {...settings.rawgApiKey}
            />
          </FormGroup>
        </Form>
      ) : null}
    </FieldSet>
  );
}

export default MetadataOptions;
