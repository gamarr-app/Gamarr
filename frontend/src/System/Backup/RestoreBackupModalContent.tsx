import { Component } from 'react';
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

interface RestoreBackupModalContentState {
  file: File | null;
  path: string;
  isRestored: boolean;
  isRestarted: boolean;
  isReloading: boolean;
}

class RestoreBackupModalContent extends Component<
  RestoreBackupModalContentProps,
  RestoreBackupModalContentState
> {
  //
  // Lifecycle

  constructor(props: RestoreBackupModalContentProps) {
    super(props);

    this.state = {
      file: null,
      path: '',
      isRestored: false,
      isRestarted: false,
      isReloading: false,
    };
  }

  componentDidUpdate(prevProps: RestoreBackupModalContentProps) {
    const { isRestoring, restoreError, isRestarting, dispatchRestart } =
      this.props;

    if (prevProps.isRestoring && !isRestoring && !restoreError) {
      this.setState({ isRestored: true }, () => {
        dispatchRestart();
      });
    }

    if (prevProps.isRestarting && !isRestarting) {
      this.setState(
        {
          isRestarted: true,
          isReloading: true,
        },
        () => {
          location.reload();
        }
      );
    }
  }

  //
  // Listeners

  onPathChange = ({ value, files }: FileInputChanged) => {
    this.setState({
      file: files ? files[0] : null,
      path: value,
    });
  };

  onRestorePress = () => {
    const { id, onRestorePress } = this.props;

    onRestorePress({
      id,
      file: this.state.file ?? undefined,
    });
  };

  //
  // Render

  render() {
    const { id, name, isRestoring, restoreError, isRestarting, onModalClose } =
      this.props;

    const { path, isRestored, isRestarted, isReloading } = this.state;

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
              onChange={this.onPathChange}
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
            onPress={this.onRestorePress}
          >
            {translate('Restore')}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

export default RestoreBackupModalContent;
