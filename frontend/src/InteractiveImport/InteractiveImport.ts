import ModelBase from 'App/ModelBase';
import Game from 'Game/Game';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';
import Rejection from 'typings/Rejection';

export interface InteractiveImportCommandOptions {
  path: string;
  folderName: string;
  gameId: number;
  releaseGroup?: string;
  quality: QualityModel;
  languages: Language[];
  indexerFlags: number;
  downloadId?: string;
  gameFileId?: number;
}

interface InteractiveImport extends ModelBase {
  path: string;
  relativePath: string;
  folderName: string;
  name: string;
  size: number;
  releaseGroup: string;
  quality: QualityModel;
  languages: Language[];
  game?: Game;
  qualityWeight: number;
  customFormats: CustomFormat[];
  indexerFlags: number;
  rejections: Rejection[];
  gameFileId?: number;
}

export default InteractiveImport;
