import { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { Backup } from 'App/State/SystemAppState';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { deleteBackup, fetchBackups } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import Backups from './Backups';

const selectBackupsState = createSelector(
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

function BackupsConnector() {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, error, items, backupExecuting } =
    useSelector(selectBackupsState);

  const prevBackupExecutingRef = useRef(backupExecuting);

  const dispatchFetchBackups = useCallback(() => {
    dispatch(fetchBackups());
  }, [dispatch]);

  const onDeleteBackupPress = useCallback(
    (id: number) => {
      dispatch(deleteBackup({ id }));
    },
    [dispatch]
  );

  const onBackupPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: commandNames.BACKUP,
      })
    );
  }, [dispatch]);

  // Fetch backups on mount
  useEffect(() => {
    dispatchFetchBackups();
  }, [dispatchFetchBackups]);

  // Refetch when backup command finishes
  useEffect(() => {
    if (prevBackupExecutingRef.current && !backupExecuting) {
      dispatchFetchBackups();
    }
    prevBackupExecutingRef.current = backupExecuting;
  }, [backupExecuting, dispatchFetchBackups]);

  return (
    <Backups
      isFetching={isFetching}
      isPopulated={isPopulated}
      error={error}
      items={items}
      backupExecuting={backupExecuting}
      onDeleteBackupPress={onDeleteBackupPress}
      onBackupPress={onBackupPress}
    />
  );
}

export default BackupsConnector;
