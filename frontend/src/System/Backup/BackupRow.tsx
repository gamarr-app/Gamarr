import { Component } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import RestoreBackupModalConnector from './RestoreBackupModalConnector';
import styles from './BackupRow.css';

interface BackupRowProps {
  id: number;
  type: string;
  name: string;
  path: string;
  size: number;
  time: string;
  onDeleteBackupPress: (id: number) => void;
}

interface BackupRowState {
  isRestoreModalOpen: boolean;
  isConfirmDeleteModalOpen: boolean;
}

class BackupRow extends Component<BackupRowProps, BackupRowState> {
  //
  // Lifecycle

  constructor(props: BackupRowProps) {
    super(props);

    this.state = {
      isRestoreModalOpen: false,
      isConfirmDeleteModalOpen: false,
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

  onDeletePress = () => {
    this.setState({ isConfirmDeleteModalOpen: true });
  };

  onConfirmDeleteModalClose = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
  };

  onConfirmDeletePress = () => {
    const { id, onDeleteBackupPress } = this.props;

    this.setState({ isConfirmDeleteModalOpen: false }, () => {
      onDeleteBackupPress(id);
    });
  };

  //
  // Render

  render() {
    const { id, type, name, path, size, time } = this.props;

    const { isRestoreModalOpen, isConfirmDeleteModalOpen } = this.state;

    let iconClassName = icons.SCHEDULED;
    let iconTooltip = translate('Scheduled');

    if (type === 'manual') {
      iconClassName = icons.INTERACTIVE;
      iconTooltip = translate('Manual');
    } else if (type === 'update') {
      iconClassName = icons.UPDATE;
      iconTooltip = translate('BeforeUpdate');
    }

    return (
      <TableRow key={id}>
        <TableRowCell className={styles.type}>
          <Icon name={iconClassName} title={iconTooltip} />
        </TableRowCell>

        <TableRowCell>
          <Link to={`${window.Gamarr.urlBase}${path}`} noRouter={true}>
            {name}
          </Link>
        </TableRowCell>

        <TableRowCell>{formatBytes(size)}</TableRowCell>

        <RelativeDateCell date={time} />

        <TableRowCell className={styles.actions}>
          <IconButton
            title={translate('RestoreBackup')}
            name={icons.RESTORE}
            onPress={this.onRestorePress}
          />

          <IconButton
            title={translate('DeleteBackup')}
            name={icons.DELETE}
            onPress={this.onDeletePress}
          />
        </TableRowCell>

        <RestoreBackupModalConnector
          isOpen={isRestoreModalOpen}
          id={id}
          name={name}
          onModalClose={this.onRestoreModalClose}
        />

        <ConfirmModal
          isOpen={isConfirmDeleteModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteBackup')}
          message={translate('DeleteBackupMessageText', {
            name,
          })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeletePress}
          onCancel={this.onConfirmDeleteModalClose}
        />
      </TableRow>
    );
  }
}

export default BackupRow;
