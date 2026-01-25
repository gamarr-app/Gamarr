export interface ImportGameSelectedGame {
  igdbId: number;
  title?: string;
  year?: number;
  studio?: string;
  [key: string]: unknown;
}

export interface ImportGameItem {
  id: string;
  term: string;
  path: string;
  relativePath: string;
  isFetching: boolean;
  isPopulated: boolean;
  isQueued: boolean;
  error: unknown;
  items: ImportGameSelectedGame[];
  selectedGame?: ImportGameSelectedGame;
  monitor?: string;
  qualityProfileId?: number;
  minimumAvailability?: string;
}

export interface ImportError {
  responseJSON?: Array<{ errorMessage: string }> | object;
}

interface ImportGameAppState {
  isLookingUpGame: boolean;
  isImporting: boolean;
  isImported: boolean;
  importError?: ImportError;
  items: ImportGameItem[];
}

export default ImportGameAppState;
