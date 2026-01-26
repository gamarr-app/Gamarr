import Game from 'Game/Game';

// The lookup API returns full Game objects, so this extends Partial<Game>
// with igdbId being the only required field for identification
export interface ImportGameSelectedGame extends Partial<Game> {
  igdbId: number;
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
  rootFolderPath?: string;
  tags?: number[];
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
