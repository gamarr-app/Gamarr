export const BOOL = 'bool';
export const BYTES = 'bytes';
export const DATE = 'date';
export const DEFAULT = 'default';
export const HISTORY_EVENT_TYPE = 'historyEventType';
export const INDEXER = 'indexer';
export const LANGUAGE = 'language';
export const PROTOCOL = 'protocol';
export const QUALITY = 'quality';
export const QUALITY_PROFILE = 'qualityProfile';
export const QUEUE_STATUS = 'queueStatus';
export const GAME = 'game';
export const RELEASE_STATUS = 'releaseStatus';
export const MINIMUM_AVAILABILITY = 'minimumAvailability';
export const TAG = 'tag';
export const IMPORTLIST = 'importList';

export type FilterBuilderValueType =
  | typeof BOOL
  | typeof BYTES
  | typeof DATE
  | typeof DEFAULT
  | typeof HISTORY_EVENT_TYPE
  | typeof INDEXER
  | typeof LANGUAGE
  | typeof PROTOCOL
  | typeof QUALITY
  | typeof QUALITY_PROFILE
  | typeof QUEUE_STATUS
  | typeof GAME
  | typeof RELEASE_STATUS
  | typeof MINIMUM_AVAILABILITY
  | typeof TAG
  | typeof IMPORTLIST;
