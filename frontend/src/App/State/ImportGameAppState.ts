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

interface ImportGameAppState {
  isLookingUpGame: boolean;
  isImporting: boolean;
  isImported: boolean;
  importError: unknown;
  items: ImportGameItem[];
}

export default ImportGameAppState;
