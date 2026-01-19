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
  // Deprecated: movie-specific ratings not used for games
  imdb?: RatingValues;
  rottenTomatoes?: RatingValues;
  trakt?: RatingValues;
}

export interface AlternativeTitle extends ModelBase {
  sourceType: string;
  title: string;
}

export interface GameAddOptions {
  monitor: GameMonitor;
  searchForGame: boolean;
}

interface Game extends ModelBase {
  igdbId: number;
  imdbId?: string;
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
}

export default Game;
