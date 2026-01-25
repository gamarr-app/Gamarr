import React, { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import {
  fetchDownloadClientOptions,
  saveDownloadClientOptions,
  setDownloadClientOptionsValue,
} from 'Store/Actions/settingsActions';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import { InputChanged } from 'typings/inputs';
import {
  OnChildStateChange,
  SetChildSave,
} from 'typings/Settings/SettingsState';
import translate from 'Utilities/String/translate';

const SECTION = 'downloadClientOptions' as const;

function createMapStateSelector() {
  return createSelector(
    (state: { settings: { advancedSettings: boolean } }) =>
      state.settings.advancedSettings,
    createSettingsSectionSelector(SECTION),
    (advancedSettings, sectionSettings) => {
      return {
        advancedSettings,
        ...sectionSettings,
      };
    }
  );
}

interface DownloadClientOptionsProps {
  setChildSave: SetChildSave;
  onChildStateChange: OnChildStateChange;
}

function DownloadClientOptions({
  setChildSave,
  onChildStateChange,
}: DownloadClientOptionsProps) {
  const dispatch = useDispatch();

  const {
    advancedSettings,
    isFetching,
    error,
    settings,
    hasSettings,
    hasPendingChanges,
    isSaving,
  } = useSelector(createMapStateSelector());

  const prevIsSaving = useRef(isSaving);
  const prevHasPendingChanges = useRef(hasPendingChanges);

  useEffect(() => {
    dispatch(fetchDownloadClientOptions());
    setChildSave(() => dispatch(saveDownloadClientOptions()));

    return () => {
      dispatch(clearPendingChanges({ section: `settings.${SECTION}` }));
    };
  }, [dispatch, setChildSave]);

  useEffect(() => {
    if (
      prevIsSaving.current !== isSaving ||
      prevHasPendingChanges.current !== hasPendingChanges
    ) {
      onChildStateChange({ isSaving, hasPendingChanges });
    }
    prevIsSaving.current = isSaving;
    prevHasPendingChanges.current = hasPendingChanges;
  }, [isSaving, hasPendingChanges, onChildStateChange]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(setDownloadClientOptionsValue({ name, value }));
    },
    [dispatch]
  );

  return (
    <div>
      {isFetching ? <LoadingIndicator /> : null}

      {!isFetching && error ? (
        <Alert kind={kinds.DANGER}>
          {translate('DownloadClientOptionsLoadError')}
        </Alert>
      ) : null}

      {hasSettings && !isFetching && !error && advancedSettings ? (
        <div>
          <FieldSet legend={translate('CompletedDownloadHandling')}>
            <Form>
              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>{translate('Enable')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="enableCompletedDownloadHandling"
                  helpText={translate(
                    'EnableCompletedDownloadHandlingHelpText'
                  )}
                  onChange={handleInputChange}
                  {...settings.enableCompletedDownloadHandling}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>
                  {translate('CheckForFinishedDownloadsInterval')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="checkForFinishedDownloadInterval"
                  min={1}
                  max={120}
                  unit="minutes"
                  helpText={translate('RefreshMonitoredIntervalHelpText')}
                  onChange={handleInputChange}
                  {...settings.checkForFinishedDownloadInterval}
                />
              </FormGroup>
            </Form>
          </FieldSet>

          <FieldSet legend={translate('FailedDownloadHandling')}>
            <Form>
              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>{translate('AutoRedownloadFailed')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="autoRedownloadFailed"
                  helpText={translate('AutoRedownloadFailedHelpText')}
                  onChange={handleInputChange}
                  {...settings.autoRedownloadFailed}
                />
              </FormGroup>

              {settings.autoRedownloadFailed.value ? (
                <FormGroup
                  advancedSettings={advancedSettings}
                  isAdvanced={true}
                  size={sizes.MEDIUM}
                >
                  <FormLabel>
                    {translate('AutoRedownloadFailedFromInteractiveSearch')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="autoRedownloadFailedFromInteractiveSearch"
                    helpText={translate(
                      'AutoRedownloadFailedFromInteractiveSearchHelpText'
                    )}
                    onChange={handleInputChange}
                    {...settings.autoRedownloadFailedFromInteractiveSearch}
                  />
                </FormGroup>
              ) : null}
            </Form>

            <Alert kind={kinds.INFO}>{translate('RemoveDownloadsAlert')}</Alert>
          </FieldSet>
        </div>
      ) : null}
    </div>
  );
}

export default DownloadClientOptions;
