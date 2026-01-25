interface SelectedGame {
  igdbId: number;
  [key: string]: unknown;
}

interface ImportItem {
  id: string;
  term: string;
  path: string;
  relativePath: string;
  isFetching: boolean;
  isPopulated: boolean;
  isQueued: boolean;
  error: unknown;
  items: SelectedGame[];
  selectedGame?: SelectedGame;
}

interface ImportGameAppState {
  isLookingUpGame: boolean;
  isImporting: boolean;
  isImported: boolean;
  importError: unknown;
  items: ImportItem[];
}

export default ImportGameAppState;
