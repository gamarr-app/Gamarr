export const QUALITY_PROFILE_ITEM = 'qualityProfileItem';
export const QUALITY_PROFILE_FORMAT_ITEM = 'qualityProfileFormatItem';
export const DELAY_PROFILE = 'delayProfile';
export const TABLE_COLUMN = 'tableColumn';

export type DragType =
  | typeof QUALITY_PROFILE_ITEM
  | typeof QUALITY_PROFILE_FORMAT_ITEM
  | typeof DELAY_PROFILE
  | typeof TABLE_COLUMN;
