import ModelBase from 'App/ModelBase';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';

export interface GameFile extends ModelBase {
  gameId: number;
  relativePath: string;
  path: string;
  size: number;
  dateAdded: string;
  sceneName: string;
  releaseGroup: string;
  edition: string;
  version?: string;
  languages: Language[];
  quality: QualityModel;
  customFormats: CustomFormat[];
  customFormatScore: number;
  indexerFlags: number;
  qualityCutoffNotMet: boolean;
}
