import ModelBase from 'App/ModelBase';
import { GameFile } from 'GameFile/GameFile';
import Language from 'Language/Language';

export type GameMonitor = 'gameOnly' | 'gameAndCollection' | 'none';

export type GameStatus =
  | 'tba'
  | 'announced'
  | 'inCinemas'
  | 'released'
  | 'deleted';

export type GameAvailability = 'announced' | 'inCinemas' | 'released';

export type CoverType = 'poster' | 'fanart' | 'headshot';

export interface Image {
  coverType: CoverType;
  url: string;
  remoteUrl: string;
}

export interface Collection {
  igdbId: number;
  title: string;
}

export type GameType =
  | 'mainGame'
  | 'dlcAddon'
  | 'expansion'
  | 'bundle'
  | 'standaloneExpansion'
  | 'mod'
  | 'episode'
  | 'season'
  | 'remake'
  | 'remaster'
  | 'expandedGame'
  | 'port'
  | 'fork'
  | 'pack'
  | 'update';

export interface GameSummary {
  id: number;
  title: string;
  steamAppId: number;
  igdbId: number;
  titleSlug: string;
  images: Image[];
}

export interface Statistics {
  gameFileCount: number;
  releaseGroups: string[];
  sizeOnDisk: number;
}

export interface RatingValues {
  votes: number;
  value: number;
}

export interface Ratings {
  igdb: RatingValues;
  metacritic: RatingValues;
}

export interface AlternativeTitle extends ModelBase {
  sourceType: string;
  title: string;
}

export interface GamePlatform {
  name: string;
  abbreviation?: string;
  family: string;
}

export interface GameAddOptions {
  monitor: GameMonitor;
  searchForGame: boolean;
}

export interface DlcReference {
  id: number;
  name: string;
}

interface Game extends ModelBase {
  /** Primary identifier - Steam App ID */
  steamAppId: number;
  /** Secondary identifier - IGDB ID */
  igdbId: number;
  /** IGDB slug for URL generation */
  igdbSlug?: string;
  sortTitle: string;
  overview: string;
  youTubeTrailerId?: string;
  monitored: boolean;
  status: GameStatus;
  title: string;
  titleSlug: string;
  originalTitle: string;
  originalLanguage: Language;
  collection: Collection;
  alternateTitles: AlternativeTitle[];
  studio: string;
  qualityProfileId: number;
  added: string;
  year: number;
  inCinemas?: string;
  physicalRelease?: string;
  digitalRelease?: string;
  releaseDate?: string;
  rootFolderPath: string;
  runtime: number;
  minimumAvailability: GameAvailability;
  path: string;
  genres: string[];
  keywords: string[];
  ratings: Ratings;
  popularity: number;
  certification: string;
  statistics?: Statistics;
  tags: number[];
  images: Image[];
  gameFileId: number;
  gameFile?: GameFile;
  hasFile: boolean;
  grabbed?: boolean;
  lastSearchTime?: string;
  isAvailable: boolean;
  isSaving?: boolean;
  addOptions: GameAddOptions;

  // Additional metadata
  developer?: string;
  publisher?: string;
  gameEngine?: string;
  gameModes: string[];
  themes: string[];
  platforms: GamePlatform[];

  // Recommendations (IGDB IDs of similar games)
  recommendations: number[];

  // DLC-related properties
  gameType: GameType;
  gameTypeDisplayName: string;
  isDlc: boolean;
  parentGameIgdbId?: number;
  parentGame?: GameSummary;
  /** IGDB DLC IDs */
  igdbDlcIds: number[];
  /** Steam DLC IDs */
  steamDlcIds: number[];
  dlcReferences: DlcReference[];
  dlcCount: number;
}

export default Game;
