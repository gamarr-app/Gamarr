import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { PendingSection } from 'typings/pending';
import General from 'typings/Settings/General';
import translate from 'Utilities/String/translate';

interface BackupSettingsProps {
  settings: PendingSection<General>;
  onInputChange: (change: InputChanged) => void;
}

function BackupSettings(props: BackupSettingsProps) {
  const { settings, onInputChange } = props;

  const { backupFolder, backupInterval, backupRetention } = settings;

  return (
    <FieldSet legend={translate('Backups')}>
      <FormGroup>
        <FormLabel>{translate('Folder')}</FormLabel>

        <FormInputGroup
          type={inputTypes.PATH}
          name="backupFolder"
          helpText={translate('BackupFolderHelpText')}
          includeFiles={false}
          onChange={onInputChange}
          {...backupFolder}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('Interval')}</FormLabel>

        <FormInputGroup
          type={inputTypes.NUMBER}
          name="backupInterval"
          unit="days"
          helpText={translate('BackupIntervalHelpText')}
          onChange={onInputChange}
          {...backupInterval}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('Retention')}</FormLabel>

        <FormInputGroup
          type={inputTypes.NUMBER}
          name="backupRetention"
          unit="days"
          helpText={translate('BackupRetentionHelpText')}
          onChange={onInputChange}
          {...backupRetention}
        />
      </FormGroup>
    </FieldSet>
  );
}

export default BackupSettings;
