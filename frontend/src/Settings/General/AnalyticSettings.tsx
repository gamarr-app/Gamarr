import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes, sizes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { PendingSection } from 'typings/pending';
import General from 'typings/Settings/General';
import translate from 'Utilities/String/translate';

interface AnalyticSettingsProps {
  settings: PendingSection<General>;
  onInputChange: (change: InputChanged) => void;
}

function AnalyticSettings(props: AnalyticSettingsProps) {
  const { settings, onInputChange } = props;

  const { analyticsEnabled } = settings;

  return (
    <FieldSet legend={translate('Analytics')}>
      <FormGroup size={sizes.MEDIUM}>
        <FormLabel>{translate('SendAnonymousUsageData')}</FormLabel>

        <FormInputGroup
          type={inputTypes.CHECK}
          name="analyticsEnabled"
          helpText={translate('AnalyticsEnabledHelpText')}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          onChange={onInputChange}
          {...analyticsEnabled}
        />
      </FormGroup>
    </FieldSet>
  );
}

export default AnalyticSettings;
