import { useCallback, useEffect, useRef, useState } from 'react';
import TextInput from 'Components/Form/TextInput';
import Icon, { IconKind, IconProps } from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, kinds } from 'Helpers/Props';
import { FileInputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import styles from './RestoreBackupModalContent.css';

interface RestoreError {
  responseJSON?: {
    message?: string;
  };
}

function getErrorMessage(error: RestoreError | unknown): string {
  if (
    !error ||
    typeof error !== 'object' ||
    !('responseJSON' in error) ||
    !(error as RestoreError).responseJSON?.message
  ) {
    return translate('ErrorRestoringBackup');
  }

  return (error as RestoreError).responseJSON!.message!;
}

function getStepIconProps(
  isExecuting: boolean,
  hasExecuted: boolean,
  error?: RestoreError | unknown
): Partial<IconProps> {
  if (isExecuting) {
    return {
      name: icons.SPINNER,
      isSpinning: true,
    };
  }

  if (hasExecuted) {
    return {
      name: icons.CHECK,
      kind: kinds.SUCCESS as IconKind,
    };
  }

  if (error) {
    return {
      name: icons.FATAL,
      kind: kinds.DANGER as IconKind,
      title: getErrorMessage(error),
    };
  }

  return {
    name: icons.PENDING,
  };
}

export interface RestoreBackupPayload {
  id?: number;
  file?: File;
}

interface RestoreBackupModalContentProps {
  id?: number;
  name?: string;
  path?: string;
  isRestoring: boolean;
  restoreError: unknown;
  isRestarting: boolean;
  dispatchRestart: () => void;
  onRestorePress: (payload: RestoreBackupPayload) => void;
  onModalClose: () => void;
}

function RestoreBackupModalContent(props: RestoreBackupModalContentProps) {
  const {
    id,
    name,
    isRestoring,
    restoreError,
    isRestarting,
    dispatchRestart,
    onRestorePress,
    onModalClose,
  } = props;

  const [file, setFile] = useState<File | null>(null);
  const [path, setPath] = useState('');
  const [isRestored, setIsRestored] = useState(false);
  const [isRestarted, setIsRestarted] = useState(false);
  const [isReloading, setIsReloading] = useState(false);

  const prevIsRestoringRef = useRef(isRestoring);
  const prevIsRestartingRef = useRef(isRestarting);

  // Handle restore completion
  useEffect(() => {
    if (prevIsRestoringRef.current && !isRestoring && !restoreError) {
      setIsRestored(true);
      dispatchRestart();
    }
    prevIsRestoringRef.current = isRestoring;
  }, [isRestoring, restoreError, dispatchRestart]);

  // Handle restart completion
  useEffect(() => {
    if (prevIsRestartingRef.current && !isRestarting) {
      setIsRestarted(true);
      setIsReloading(true);
      location.reload();
    }
    prevIsRestartingRef.current = isRestarting;
  }, [isRestarting]);

  const onPathChange = useCallback(({ value, files }: FileInputChanged) => {
    setFile(files ? files[0] : null);
    setPath(value);
  }, []);

  const handleRestorePress = useCallback(() => {
    onRestorePress({
      id,
      file: file ?? undefined,
    });
  }, [id, file, onRestorePress]);

  const isRestoreDisabled =
    (!id && !path) || isRestoring || isRestarting || isReloading;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>Restore Backup</ModalHeader>

      <ModalBody>
        {!!id &&
          translate('WouldYouLikeToRestoreBackup', {
            name: name || '',
          })}

        {!id && (
          <TextInput
            type="file"
            name="path"
            value={path}
            onChange={onPathChange}
          />
        )}

        <div className={styles.steps}>
          <div className={styles.step}>
            <div className={styles.stepState}>
              <Icon
                size={20}
                name={icons.PENDING}
                {...getStepIconProps(isRestoring, isRestored, restoreError)}
              />
            </div>

            <div>{translate('Restore')}</div>
          </div>

          <div className={styles.step}>
            <div className={styles.stepState}>
              <Icon
                size={20}
                name={icons.PENDING}
                {...getStepIconProps(isRestarting, isRestarted)}
              />
            </div>

            <div>{translate('Restart')}</div>
          </div>

          <div className={styles.step}>
            <div className={styles.stepState}>
              <Icon
                size={20}
                name={icons.PENDING}
                {...getStepIconProps(isReloading, false)}
              />
            </div>

            <div>{translate('Reload')}</div>
          </div>
        </div>
      </ModalBody>

      <ModalFooter>
        <div className={styles.additionalInfo}>
          {translate('RestartReloadNote')}
        </div>

        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <SpinnerButton
          kind={kinds.WARNING}
          isDisabled={isRestoreDisabled}
          isSpinning={isRestoring}
          onPress={handleRestorePress}
        >
          {translate('Restore')}
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default RestoreBackupModalContent;
