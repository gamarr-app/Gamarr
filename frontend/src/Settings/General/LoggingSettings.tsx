import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { PendingSection } from 'typings/pending';
import General from 'typings/Settings/General';
import translate from 'Utilities/String/translate';

const logLevelOptions = [
  {
    key: 'info',
    get value() {
      return translate('Info');
    },
  },
  {
    key: 'debug',
    get value() {
      return translate('Debug');
    },
  },
  {
    key: 'trace',
    get value() {
      return translate('Trace');
    },
  },
];

interface LoggingSettingsProps {
  settings: PendingSection<General>;
  onInputChange: (change: InputChanged) => void;
}

function LoggingSettings(props: LoggingSettingsProps) {
  const { settings, onInputChange } = props;

  const { logLevel, logSizeLimit } = settings;

  return (
    <FieldSet legend={translate('Logging')}>
      <FormGroup>
        <FormLabel>{translate('LogLevel')}</FormLabel>

        <FormInputGroup
          type={inputTypes.SELECT}
          name="logLevel"
          values={logLevelOptions}
          helpTextWarning={
            logLevel.value === 'trace'
              ? translate('LogLevelTraceHelpTextWarning')
              : undefined
          }
          onChange={onInputChange}
          {...logLevel}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('LogSizeLimit')}</FormLabel>

        <FormInputGroup
          type={inputTypes.NUMBER}
          name="logSizeLimit"
          min={1}
          max={10}
          unit="MB"
          helpText={translate('LogSizeLimitHelpText')}
          onChange={onInputChange}
          {...logSizeLimit}
        />
      </FormGroup>
    </FieldSet>
  );
}

export default LoggingSettings;
