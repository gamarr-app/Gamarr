import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { Backup } from 'App/State/SystemAppState';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { deleteBackup, fetchBackups } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import { AppDispatch } from 'Store/thunks';
import Backups from './Backups';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.system.backups,
    createCommandExecutingSelector(commandNames.BACKUP),
    (backups, backupExecuting) => {
      const { isFetching, isPopulated, error, items } = backups;

      return {
        isFetching,
        isPopulated,
        error,
        items: items as Backup[],
        backupExecuting,
      };
    }
  );
}

function createMapDispatchToProps(dispatch: AppDispatch) {
  return {
    dispatchFetchBackups() {
      dispatch(fetchBackups());
    },

    onDeleteBackupPress(id: number) {
      dispatch(deleteBackup({ id }));
    },

    onBackupPress() {
      dispatch(
        executeCommand({
          name: commandNames.BACKUP,
        })
      );
    },
  };
}

type StateProps = ReturnType<ReturnType<typeof createMapStateToProps>>;
type DispatchProps = ReturnType<typeof createMapDispatchToProps>;
type BackupsConnectorProps = StateProps & DispatchProps;

class BackupsConnector extends Component<BackupsConnectorProps> {
  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchBackups();
  }

  componentDidUpdate(prevProps: BackupsConnectorProps) {
    if (prevProps.backupExecuting && !this.props.backupExecuting) {
      this.props.dispatchFetchBackups();
    }
  }

  //
  // Render

  render() {
    return <Backups {...this.props} />;
  }
}

export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(BackupsConnector);
