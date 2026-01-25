import React from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { Pending } from 'typings/pending';
import translate from 'Utilities/String/translate';
import styles from './NotificationEventItems.css';

interface NotificationEventItem {
  onGrab: Pending<boolean>;
  onDownload: Pending<boolean>;
  onUpgrade: Pending<boolean>;
  onRename: Pending<boolean>;
  onGameAdded: Pending<boolean>;
  onGameDelete: Pending<boolean>;
  onGameFileDelete: Pending<boolean>;
  onGameFileDeleteForUpgrade: Pending<boolean>;
  onHealthIssue: Pending<boolean>;
  onHealthRestored: Pending<boolean>;
  onApplicationUpdate: Pending<boolean>;
  onManualInteractionRequired: Pending<boolean>;
  supportsOnGrab: Pending<boolean>;
  supportsOnDownload: Pending<boolean>;
  supportsOnUpgrade: Pending<boolean>;
  supportsOnRename: Pending<boolean>;
  supportsOnGameAdded: Pending<boolean>;
  supportsOnGameDelete: Pending<boolean>;
  supportsOnGameFileDelete: Pending<boolean>;
  supportsOnGameFileDeleteForUpgrade: Pending<boolean>;
  supportsOnApplicationUpdate: Pending<boolean>;
  supportsOnManualInteractionRequired: Pending<boolean>;
  supportsOnHealthIssue: Pending<boolean>;
  supportsOnHealthRestored: Pending<boolean>;
  includeHealthWarnings: Pending<boolean>;
}

interface NotificationEventItemsProps {
  item: NotificationEventItem;
  onInputChange: (change: InputChanged) => void;
}

function NotificationEventItems(props: NotificationEventItemsProps) {
  const { item, onInputChange } = props;

  const {
    onGrab,
    onDownload,
    onUpgrade,
    onRename,
    onGameAdded,
    onGameDelete,
    onGameFileDelete,
    onGameFileDeleteForUpgrade,
    onHealthIssue,
    onHealthRestored,
    onApplicationUpdate,
    onManualInteractionRequired,
    supportsOnGrab,
    supportsOnDownload,
    supportsOnUpgrade,
    supportsOnRename,
    supportsOnGameAdded,
    supportsOnGameDelete,
    supportsOnGameFileDelete,
    supportsOnGameFileDeleteForUpgrade,
    supportsOnApplicationUpdate,
    supportsOnManualInteractionRequired,
    supportsOnHealthIssue,
    supportsOnHealthRestored,
    includeHealthWarnings,
  } = item;

  return (
    <FormGroup>
      <FormLabel>{translate('NotificationTriggers')}</FormLabel>
      <div>
        <FormInputHelpText
          text={translate('NotificationTriggersHelpText')}
          link="https://wiki.servarr.com/gamarr/settings#connections"
        />
        <div className={styles.events}>
          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onGrab"
              helpText={translate('OnGrab')}
              isDisabled={!supportsOnGrab.value}
              {...onGrab}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onDownload"
              helpText={translate('OnFileImport')}
              isDisabled={!supportsOnDownload.value}
              {...onDownload}
              onChange={onInputChange}
            />
          </div>

          {onDownload.value && (
            <div>
              <FormInputGroup
                type={inputTypes.CHECK}
                name="onUpgrade"
                helpText={translate('OnFileUpgrade')}
                isDisabled={!supportsOnUpgrade.value}
                {...onUpgrade}
                onChange={onInputChange}
              />
            </div>
          )}

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onRename"
              helpText={translate('OnRename')}
              isDisabled={!supportsOnRename.value}
              {...onRename}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onGameAdded"
              helpText={translate('OnGameAdded')}
              isDisabled={!supportsOnGameAdded.value}
              {...onGameAdded}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onGameDelete"
              helpText={translate('OnGameDelete')}
              isDisabled={!supportsOnGameDelete.value}
              {...onGameDelete}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onGameFileDelete"
              helpText={translate('OnGameFileDelete')}
              isDisabled={!supportsOnGameFileDelete.value}
              {...onGameFileDelete}
              onChange={onInputChange}
            />
          </div>

          {onGameFileDelete.value && (
            <div>
              <FormInputGroup
                type={inputTypes.CHECK}
                name="onGameFileDeleteForUpgrade"
                helpText={translate('OnGameFileDeleteForUpgrade')}
                isDisabled={!supportsOnGameFileDeleteForUpgrade.value}
                {...onGameFileDeleteForUpgrade}
                onChange={onInputChange}
              />
            </div>
          )}

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onHealthIssue"
              helpText={translate('OnHealthIssue')}
              isDisabled={!supportsOnHealthIssue.value}
              {...onHealthIssue}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onHealthRestored"
              helpText={translate('OnHealthRestored')}
              isDisabled={!supportsOnHealthRestored.value}
              {...onHealthRestored}
              onChange={onInputChange}
            />
          </div>

          {(onHealthIssue.value || onHealthRestored.value) && (
            <div>
              <FormInputGroup
                type={inputTypes.CHECK}
                name="includeHealthWarnings"
                helpText={translate('IncludeHealthWarnings')}
                isDisabled={!supportsOnHealthIssue.value}
                {...includeHealthWarnings}
                onChange={onInputChange}
              />
            </div>
          )}

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onApplicationUpdate"
              helpText={translate('OnApplicationUpdate')}
              isDisabled={!supportsOnApplicationUpdate.value}
              {...onApplicationUpdate}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onManualInteractionRequired"
              helpText={translate('OnManualInteractionRequired')}
              isDisabled={!supportsOnManualInteractionRequired.value}
              {...onManualInteractionRequired}
              onChange={onInputChange}
            />
          </div>
        </div>
      </div>
    </FormGroup>
  );
}

export default NotificationEventItems;
