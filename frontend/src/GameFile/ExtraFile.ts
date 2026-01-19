import ModelBase from 'App/ModelBase';

export type ExtraFileType = 'subtitle' | 'metadata' | 'other';

export interface ExtraFile extends ModelBase {
  gameId: number;
  gameFileId?: number;
  relativePath: string;
  extension: string;
  type: ExtraFileType;
}
