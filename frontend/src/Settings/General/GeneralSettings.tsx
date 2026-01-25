import _ from 'lodash';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { kinds } from 'Helpers/Props';
import SettingsToolbar from 'Settings/SettingsToolbar';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  fetchGeneralSettings,
  saveGeneralSettings,
  setGeneralSettingsValue,
} from 'Store/Actions/settingsActions';
import { restart } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import { InputChanged } from 'typings/inputs';
import { Failure, PendingSection } from 'typings/pending';
import General from 'typings/Settings/General';
import translate from 'Utilities/String/translate';
import AnalyticSettings from './AnalyticSettings';
import BackupSettings from './BackupSettings';
import HostSettings from './HostSettings';
import LoggingSettings from './LoggingSettings';
import ProxySettings from './ProxySettings';
import SecuritySettings from './SecuritySettings';
import UpdateSettings from './UpdateSettings';
import styles from './GeneralSettings.css';

const SECTION = 'general';

const requiresRestartKeys = [
  'bindAddress',
  'port',
  'urlBase',
  'instanceName',
  'enableSsl',
  'sslPort',
  'sslCertPath',
  'sslCertPassword',
];

function createGeneralSettingsSelector() {
  return createSelector(
    createSettingsSectionSelector(SECTION),
    createCommandExecutingSelector(commandNames.RESET_API_KEY),
    createSystemStatusSelector(),
    (sectionSettings, isResettingApiKey, systemStatus) => {
      return {
        isResettingApiKey,
        isWindows: systemStatus.isWindows,
        isWindowsService:
          systemStatus.isWindows && systemStatus.mode === 'service',
        mode: systemStatus.mode,
        packageUpdateMechanism: systemStatus.packageUpdateMechanism,
        ...sectionSettings,
      };
    }
  );
}

function GeneralSettings() {
  const dispatch = useDispatch();
  const showAdvancedSettings = useSelector(
    (state: { settings: { advancedSettings: boolean } }) =>
      state.settings.advancedSettings
  );

  const {
    isFetching,
    isPopulated,
    isSaving,
    saveError,
    error,
    settings,
    hasSettings,
    hasPendingChanges,
    isResettingApiKey,
    isWindows,
    isWindowsService,
    mode,
    packageUpdateMechanism,
  } = useSelector(createGeneralSettingsSelector());

  const [isRestartRequiredModalOpen, setIsRestartRequiredModalOpen] =
    useState(false);

  const prevIsResettingApiKey = useRef(isResettingApiKey);
  const prevIsSaving = useRef(isSaving);
  const prevSettings = useRef(settings);

  useEffect(() => {
    if (!isResettingApiKey && prevIsResettingApiKey.current) {
      dispatch(fetchGeneralSettings());
      setIsRestartRequiredModalOpen(true);
    }
    prevIsResettingApiKey.current = isResettingApiKey;
  }, [isResettingApiKey, dispatch]);

  useEffect(() => {
    if (!isSaving && !saveError && prevIsSaving.current) {
      const typedSettings = settings as PendingSection<General>;
      const typedPrevSettings = prevSettings.current as
        | PendingSection<General>
        | undefined;

      const pendingRestart = _.some(requiresRestartKeys, (key) => {
        const setting = (
          typedSettings as unknown as Record<
            string,
            { value: unknown; previousValue?: unknown }
          >
        )[key];
        const prevSetting = (
          typedPrevSettings as unknown as
            | Record<string, { value: unknown; previousValue?: unknown }>
            | undefined
        )?.[key];

        if (!setting || !prevSetting) {
          return false;
        }

        const previousValue = prevSetting.previousValue;
        const value = setting.value;

        return previousValue != null && previousValue !== value;
      });

      if (pendingRestart) {
        setIsRestartRequiredModalOpen(true);
      }
    }
    prevIsSaving.current = isSaving;
    prevSettings.current = settings;
  }, [isSaving, saveError, settings]);

  useEffect(() => {
    dispatch(fetchGeneralSettings());

    return () => {
      dispatch(clearPendingChanges({ section: `settings.${SECTION}` }));
    };
  }, [dispatch]);

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setGeneralSettingsValue(change));
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveGeneralSettings());
  }, [dispatch]);

  const handleConfirmResetApiKey = useCallback(() => {
    dispatch(executeCommand({ name: commandNames.RESET_API_KEY }));
  }, [dispatch]);

  const handleConfirmRestart = useCallback(() => {
    setIsRestartRequiredModalOpen(false);
    dispatch(restart());
  }, [dispatch]);

  const handleCloseRestartModal = useCallback(() => {
    setIsRestartRequiredModalOpen(false);
  }, []);

  return (
    <PageContent
      className={styles.generalSettings}
      title={translate('GeneralSettings')}
    >
      <SettingsToolbar
        isSaving={isSaving}
        hasPendingChanges={hasPendingChanges}
        onSavePress={handleSavePress}
      />

      <PageContentBody>
        {isFetching && !isPopulated ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>
            {translate('GeneralSettingsLoadError')}
          </Alert>
        ) : null}

        {hasSettings && isPopulated && !error ? (
          <Form id="generalSettings">
            <HostSettings
              advancedSettings={showAdvancedSettings}
              settings={settings}
              isWindows={isWindows}
              mode={mode}
              onInputChange={handleInputChange}
            />

            <SecuritySettings
              settings={
                settings as unknown as Record<
                  string,
                  {
                    value: string;
                    errors?: Failure[];
                    warnings?: Failure[];
                    previousValue?: string;
                  }
                >
              }
              isResettingApiKey={isResettingApiKey}
              onInputChange={handleInputChange}
              onConfirmResetApiKey={handleConfirmResetApiKey}
            />

            <ProxySettings
              settings={settings}
              onInputChange={handleInputChange}
            />

            <LoggingSettings
              advancedSettings={showAdvancedSettings}
              settings={settings}
              onInputChange={handleInputChange}
            />

            <AnalyticSettings
              settings={settings}
              onInputChange={handleInputChange}
            />

            <UpdateSettings
              advancedSettings={showAdvancedSettings}
              settings={settings}
              isWindows={isWindows}
              packageUpdateMechanism={packageUpdateMechanism}
              onInputChange={handleInputChange}
            />

            <BackupSettings
              advancedSettings={showAdvancedSettings}
              settings={settings}
              onInputChange={handleInputChange}
            />
          </Form>
        ) : null}
      </PageContentBody>

      <ConfirmModal
        isOpen={isRestartRequiredModalOpen}
        kind={kinds.DANGER}
        title={translate('RestartGamarr')}
        message={`${translate('RestartRequiredToApplyChanges')} ${
          isWindowsService ? translate('RestartRequiredWindowsService') : ''
        }`}
        cancelLabel={translate('RestartLater')}
        confirmLabel={translate('RestartNow')}
        onConfirm={handleConfirmRestart}
        onCancel={handleCloseRestartModal}
      />
    </PageContent>
  );
}

export default GeneralSettings;
