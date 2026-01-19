import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from './CustomFormat';

export type HistoryEventType =
  | 'grabbed'
  | 'downloadFolderImported'
  | 'downloadFailed'
  | 'gameFileDeleted'
  | 'gameFolderImported'
  | 'gameFileRenamed'
  | 'downloadIgnored';

export interface GrabbedHistoryData {
  indexer: string;
  nzbInfoUrl: string;
  releaseGroup: string;
  age: string;
  ageHours: string;
  ageMinutes: string;
  publishedDate: string;
  downloadClient: string;
  downloadClientName: string;
  size: string;
  downloadUrl: string;
  guid: string;
  igdbId: string;
  steamAppId: string;
  protocol: string;
  customFormatScore?: string;
  gameMatchType: string;
  releaseSource: string;
  indexerFlags: string;
}

export interface DownloadFailedHistory {
  message: string;
  indexer?: string;
}

export interface DownloadFolderImportedHistory {
  customFormatScore?: string;
  downloadClient: string;
  downloadClientName: string;
  droppedPath: string;
  importedPath: string;
  size: string;
}

export interface GameFileDeletedHistory {
  customFormatScore?: string;
  reason: 'Manual' | 'MissingFromDisk' | 'Upgrade';
  size: string;
}

export interface GameFileRenamedHistory {
  sourcePath: string;
  sourceRelativePath: string;
  path: string;
  relativePath: string;
}

export interface DownloadIgnoredHistory {
  message: string;
}

export type HistoryData =
  | GrabbedHistoryData
  | DownloadFailedHistory
  | DownloadFolderImportedHistory
  | GameFileDeletedHistory
  | GameFileRenamedHistory
  | DownloadIgnoredHistory;

export default interface History {
  gameId: number;
  sourceTitle: string;
  languages: Language[];
  quality: QualityModel;
  customFormats: CustomFormat[];
  customFormatScore: number;
  qualityCutoffNotMet: boolean;
  date: string;
  downloadId: string;
  eventType: HistoryEventType;
  data: HistoryData;
  id: number;
}
