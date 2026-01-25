import { Component } from 'react';
import Icon, { IconName } from 'Components/Icon';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import Column from 'Components/Table/Column';
import TableRowButton from 'Components/Table/TableRowButton';
import { icons } from 'Helpers/Props';
import LogsTableDetailsModal from './LogsTableDetailsModal';
import styles from './LogsTableRow.css';

function getIconName(level: string): IconName {
  switch (level) {
    case 'trace':
    case 'debug':
    case 'info':
      return icons.INFO;
    case 'warn':
      return icons.DANGER;
    case 'error':
      return icons.BUG;
    case 'fatal':
      return icons.FATAL;
    default:
      return icons.UNKNOWN;
  }
}

interface LogsTableRowProps {
  level: string;
  time: string;
  logger: string;
  message: string;
  exception?: string;
  columns: Column[];
}

interface LogsTableRowState {
  isDetailsModalOpen: boolean;
}

class LogsTableRow extends Component<LogsTableRowProps, LogsTableRowState> {
  //
  // Lifecycle

  constructor(props: LogsTableRowProps) {
    super(props);

    this.state = {
      isDetailsModalOpen: false,
    };
  }

  //
  // Listeners

  onPress = () => {
    // Don't re-open the modal if it's already open
    if (!this.state.isDetailsModalOpen) {
      this.setState({ isDetailsModalOpen: true });
    }
  };

  onModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  //
  // Render

  render() {
    const { level, time, logger, message, exception, columns } = this.props;

    return (
      <TableRowButton onPress={this.onPress}>
        {columns.map((column) => {
          const { name, isVisible } = column;

          if (!isVisible) {
            return null;
          }

          if (name === 'level') {
            return (
              <TableRowCell key={name} className={styles.level}>
                <Icon
                  className={styles[level as keyof typeof styles]}
                  name={getIconName(level)}
                  title={level}
                />
              </TableRowCell>
            );
          }

          if (name === 'time') {
            return <RelativeDateCell key={name} date={time} />;
          }

          if (name === 'logger') {
            return <TableRowCell key={name}>{logger}</TableRowCell>;
          }

          if (name === 'message') {
            return <TableRowCell key={name}>{message}</TableRowCell>;
          }

          if (name === 'actions') {
            return <TableRowCell key={name} className={styles.actions} />;
          }

          return null;
        })}

        <LogsTableDetailsModal
          isOpen={this.state.isDetailsModalOpen}
          message={message}
          exception={exception}
          onModalClose={this.onModalClose}
        />
      </TableRowButton>
    );
  }
}

export default LogsTableRow;
