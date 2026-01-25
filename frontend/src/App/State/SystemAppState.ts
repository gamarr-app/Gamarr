import Column from 'Components/Table/Column';
import DiskSpace from 'typings/DiskSpace';
import Health from 'typings/Health';
import LogFile from 'typings/LogFile';
import SystemStatus from 'typings/SystemStatus';
import Task from 'typings/Task';
import Update from 'typings/Update';
import AppSectionState, { AppSectionItemState } from './AppSectionState';

export interface Backup {
  id: number;
  type: string;
  name: string;
  path: string;
  size: number;
  time: string;
}

export interface LogItem {
  id: number;
  level: string;
  time: string;
  logger: string;
  message: string;
  exception?: string;
}

export interface LogFilter {
  key: string;
  label: () => string;
  filters: { key: string; value: string; type: string }[];
}

export interface BackupsAppState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isRestoring: boolean;
  restoreError: unknown;
  isDeleting: boolean;
  deleteError: unknown;
  items: Backup[];
}

export interface LogsAppState {
  isFetching: boolean;
  isPopulated: boolean;
  pageSize: number;
  sortKey: string;
  sortDirection: string;
  error: unknown;
  items: LogItem[];
  columns: Column[];
  selectedFilterKey: string;
  filters: LogFilter[];
  page?: number;
  totalPages?: number;
  totalRecords?: number;
}

export type DiskSpaceAppState = AppSectionState<DiskSpace>;
export type HealthAppState = AppSectionState<Health>;
export type SystemStatusAppState = AppSectionItemState<SystemStatus>;
export type TaskAppState = AppSectionState<Task>;
export type LogFilesAppState = AppSectionState<LogFile>;
export type UpdateAppState = AppSectionState<Update>;

interface SystemAppState {
  backups: BackupsAppState;
  diskSpace: DiskSpaceAppState;
  health: HealthAppState;
  logFiles: LogFilesAppState;
  logs: LogsAppState;
  status: SystemStatusAppState;
  tasks: TaskAppState;
  updateLogFiles: LogFilesAppState;
  updates: UpdateAppState;
}

export default SystemAppState;
