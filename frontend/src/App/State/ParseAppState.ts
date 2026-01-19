import ModelBase from 'App/ModelBase';
import { AppSectionItemState } from 'App/State/AppSectionState';
import Game from 'Game/Game';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';

export interface ParsedGameInfo {
  releaseTitle: string;
  originalTitle: string;
  gameTitle: string;
  gameTitles: string[];
  year: number;
  quality: QualityModel;
  languages: Language[];
  releaseHash: string;
  releaseGroup?: string;
  edition?: string;
  igdbId?: number;
  steamAppId?: number;
}

export interface ParseModel extends ModelBase {
  title: string;
  parsedGameInfo: ParsedGameInfo;
  game?: Game;
  languages?: Language[];
  customFormats?: CustomFormat[];
  customFormatScore?: number;
}

type ParseAppState = AppSectionItemState<ParseModel>;

export default ParseAppState;
