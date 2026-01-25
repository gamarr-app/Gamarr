import { GameMonitor } from 'Game/Game';

interface AddGameDefaults {
  rootFolderPath: string;
  monitor: GameMonitor;
  qualityProfileId: number;
  minimumAvailability: string;
  searchForGame: boolean;
  tags: number[];
}

interface AddGameAppState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isAdding: boolean;
  isAdded: boolean;
  addError: unknown;
  items: unknown[];
  defaults: AddGameDefaults;
}

export default AddGameAppState;
