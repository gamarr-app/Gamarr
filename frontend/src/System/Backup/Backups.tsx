import React, { Component } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import BackupRow from './BackupRow';
import RestoreBackupModalConnector from './RestoreBackupModalConnector';
import styles from './Backups.css';

const columns: Column[] = [
  {
    name: 'type',
    label: '',
    isVisible: true,
  },
  {
    name: 'name',
    label: () => translate('Name'),
    isVisible: true,
  },
  {
    name: 'size',
    label: () => translate('Size'),
    isVisible: true,
  },
  {
    name: 'time',
    label: () => translate('Time'),
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

export interface BackupItem {
  id: number;
  type: string;
  name: string;
  path: string;
  size: number;
  time: string;
}

interface BackupsProps {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: BackupItem[];
  backupExecuting: boolean;
  onBackupPress: () => void;
  onDeleteBackupPress: (id: number) => void;
}

interface BackupsState {
  isRestoreModalOpen: boolean;
}

class Backups extends Component<BackupsProps, BackupsState> {
  //
  // Lifecycle

  constructor(props: BackupsProps) {
    super(props);

    this.state = {
      isRestoreModalOpen: false,
    };
  }

  //
  // Listeners

  onRestorePress = () => {
    this.setState({ isRestoreModalOpen: true });
  };

  onRestoreModalClose = () => {
    this.setState({ isRestoreModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      items,
      backupExecuting,
      onBackupPress,
      onDeleteBackupPress,
    } = this.props;

    const hasBackups = isPopulated && !!items.length;
    const noBackups = isPopulated && !items.length;

    return (
      <PageContent className={styles.backups} title={translate('Backups')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('BackupNow')}
              iconName={icons.BACKUP}
              isSpinning={backupExecuting}
              onPress={onBackupPress}
            />

            <PageToolbarButton
              label={translate('RestoreBackup')}
              iconName={icons.RESTORE}
              onPress={this.onRestorePress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody>
          {isFetching && !isPopulated && <LoadingIndicator />}

          {!isFetching && !!error && (
            <Alert kind={kinds.DANGER}>{translate('BackupsLoadError')}</Alert>
          )}

          {noBackups && (
            <Alert kind={kinds.INFO}>
              {translate('NoBackupsAreAvailable')}
            </Alert>
          )}

          {hasBackups && (
            <Table columns={columns}>
              <TableBody>
                {items.map((item) => {
                  const { id, type, name, path, size, time } = item;

                  return (
                    <BackupRow
                      key={id}
                      id={id}
                      type={type}
                      name={name}
                      path={path}
                      size={size}
                      time={time}
                      onDeleteBackupPress={onDeleteBackupPress}
                    />
                  );
                })}
              </TableBody>
            </Table>
          )}
        </PageContentBody>

        <RestoreBackupModalConnector
          isOpen={this.state.isRestoreModalOpen}
          onModalClose={this.onRestoreModalClose}
        />
      </PageContent>
    );
  }
}

export default Backups;
