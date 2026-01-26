import { FocusEvent, useCallback, useState } from 'react';
import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import ClipboardButton from 'Components/Link/ClipboardButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { Failure } from 'typings/pending';
import translate from 'Utilities/String/translate';

export const authenticationMethodOptions = [
  {
    key: 'none',
    get value() {
      return translate('None');
    },
    isDisabled: true,
  },
  {
    key: 'external',
    get value() {
      return translate('External');
    },
    isHidden: true,
  },
  {
    key: 'basic',
    get value() {
      return translate('AuthBasic');
    },
    isDisabled: true,
    isHidden: true,
  },
  {
    key: 'forms',
    get value() {
      return translate('AuthForm');
    },
  },
];

export const authenticationRequiredOptions = [
  {
    key: 'enabled',
    get value() {
      return translate('Enabled');
    },
  },
  {
    key: 'disabledForLocalAddresses',
    get value() {
      return translate('DisabledForLocalAddresses');
    },
  },
];

const certificateValidationOptions = [
  {
    key: 'enabled',
    get value() {
      return translate('Enabled');
    },
  },
  {
    key: 'disabledForLocalAddresses',
    get value() {
      return translate('DisabledForLocalAddresses');
    },
  },
  {
    key: 'disabled',
    get value() {
      return translate('Disabled');
    },
  },
];

interface SettingValue<T = string> {
  value: T;
  errors?: Failure[];
  warnings?: Failure[];
  previousValue?: T;
}

interface SecuritySettingsRequiredKeys {
  authenticationMethod: SettingValue<string>;
  authenticationRequired: SettingValue<string>;
  username: SettingValue<string>;
  password: SettingValue<string>;
  passwordConfirmation: SettingValue<string>;
  apiKey: SettingValue<string>;
  certificateValidation: SettingValue<string>;
}

interface SecuritySettingsProps {
  settings: SecuritySettingsRequiredKeys;
  isResettingApiKey: boolean;
  onInputChange: (change: InputChanged) => void;
  onConfirmResetApiKey: () => void;
}

function SecuritySettings({
  settings,
  isResettingApiKey,
  onInputChange,
  onConfirmResetApiKey,
}: SecuritySettingsProps) {
  const [isConfirmApiKeyResetModalOpen, setIsConfirmApiKeyResetModalOpen] =
    useState(false);

  const {
    authenticationMethod,
    authenticationRequired,
    username,
    password,
    passwordConfirmation,
    apiKey,
    certificateValidation,
  } = settings;

  const authenticationEnabled =
    authenticationMethod && authenticationMethod.value !== 'none';

  const handleApikeyFocus = useCallback(
    (event: FocusEvent<HTMLInputElement>) => {
      event.target.select();
    },
    []
  );

  const handleResetApiKeyPress = useCallback(() => {
    setIsConfirmApiKeyResetModalOpen(true);
  }, []);

  const handleConfirmResetApiKey = useCallback(() => {
    setIsConfirmApiKeyResetModalOpen(false);
    onConfirmResetApiKey();
  }, [onConfirmResetApiKey]);

  const handleCloseResetApiKeyModal = useCallback(() => {
    setIsConfirmApiKeyResetModalOpen(false);
  }, []);

  return (
    <FieldSet legend={translate('Security')}>
      <FormGroup>
        <FormLabel>{translate('Authentication')}</FormLabel>

        <FormInputGroup
          type={inputTypes.SELECT}
          name="authenticationMethod"
          values={authenticationMethodOptions}
          helpText={translate('AuthenticationMethodHelpText')}
          helpTextWarning={translate('AuthenticationRequiredWarning')}
          onChange={onInputChange}
          {...authenticationMethod}
        />
      </FormGroup>

      {authenticationEnabled ? (
        <FormGroup>
          <FormLabel>{translate('AuthenticationRequired')}</FormLabel>

          <FormInputGroup
            type={inputTypes.SELECT}
            name="authenticationRequired"
            values={authenticationRequiredOptions}
            helpText={translate('AuthenticationRequiredHelpText')}
            onChange={onInputChange}
            {...authenticationRequired}
          />
        </FormGroup>
      ) : null}

      {authenticationEnabled ? (
        <FormGroup>
          <FormLabel>{translate('Username')}</FormLabel>

          <FormInputGroup
            type={inputTypes.TEXT}
            name="username"
            onChange={onInputChange}
            {...username}
          />
        </FormGroup>
      ) : null}

      {authenticationEnabled ? (
        <FormGroup>
          <FormLabel>{translate('Password')}</FormLabel>

          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="password"
            onChange={onInputChange}
            {...password}
          />
        </FormGroup>
      ) : null}

      {authenticationEnabled ? (
        <FormGroup>
          <FormLabel>{translate('PasswordConfirmation')}</FormLabel>

          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="passwordConfirmation"
            onChange={onInputChange}
            {...passwordConfirmation}
          />
        </FormGroup>
      ) : null}

      <FormGroup>
        <FormLabel>{translate('ApiKey')}</FormLabel>

        <FormInputGroup
          type={inputTypes.TEXT}
          name="apiKey"
          readOnly={true}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          buttons={[
            <ClipboardButton
              key="copy"
              value={apiKey.value}
              kind={kinds.DEFAULT}
            />,

            <FormInputButton
              key="reset"
              kind={kinds.DANGER}
              onPress={handleResetApiKeyPress}
            >
              <Icon name={icons.REFRESH} isSpinning={isResettingApiKey} />
            </FormInputButton>,
          ]}
          onChange={onInputChange}
          onFocus={handleApikeyFocus}
          {...apiKey}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('CertificateValidation')}</FormLabel>

        <FormInputGroup
          type={inputTypes.SELECT}
          name="certificateValidation"
          values={certificateValidationOptions}
          helpText={translate('CertificateValidationHelpText')}
          onChange={onInputChange}
          {...certificateValidation}
        />
      </FormGroup>

      <ConfirmModal
        isOpen={isConfirmApiKeyResetModalOpen}
        kind={kinds.DANGER}
        title={translate('ResetAPIKey')}
        message={translate('ResetAPIKeyMessageText')}
        confirmLabel={translate('Reset')}
        onConfirm={handleConfirmResetApiKey}
        onCancel={handleCloseResetApiKeyModal}
      />
    </FieldSet>
  );
}

export default SecuritySettings;
