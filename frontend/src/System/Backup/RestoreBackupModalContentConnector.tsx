import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { restart, restoreBackup } from 'Store/Actions/systemActions';
import { AppDispatch } from 'Store/thunks';
import RestoreBackupModalContent, {
  RestoreBackupPayload,
} from './RestoreBackupModalContent';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.system.backups,
    (state: AppState) => state.app.isRestarting,
    (backups, isRestarting) => {
      const { isRestoring, restoreError } = backups;

      return {
        isRestoring,
        restoreError,
        isRestarting,
      };
    }
  );
}

function createMapDispatchToProps(dispatch: AppDispatch) {
  return {
    onRestorePress(payload: RestoreBackupPayload) {
      dispatch(restoreBackup(payload));
    },

    dispatchRestart() {
      dispatch(restart());
    },
  };
}

export default connect(
  createMapStateToProps,
  createMapDispatchToProps
)(RestoreBackupModalContent);
