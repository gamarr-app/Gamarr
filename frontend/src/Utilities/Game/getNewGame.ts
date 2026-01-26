import { GameAddOptions, GameAvailability, GameMonitor } from 'Game/Game';

export interface NewGamePayload {
  rootFolderPath: string;
  monitor: GameMonitor;
  qualityProfileId: number;
  minimumAvailability: GameAvailability;
  tags: number[];
  searchForGame?: boolean;
}

// Properties added by getNewGame to any game-like object
interface NewGameAddedProps {
  addOptions: GameAddOptions;
  monitored: boolean;
  qualityProfileId: number;
  minimumAvailability: GameAvailability;
  rootFolderPath: string;
  tags: number[];
}

// Input type - accepts any object, returns it with added properties
function getNewGame<T extends object>(
  game: T,
  payload: NewGamePayload
): T & NewGameAddedProps {
  const {
    rootFolderPath,
    monitor,
    qualityProfileId,
    minimumAvailability,
    tags,
    searchForGame = false,
  } = payload;

  const addOptions: GameAddOptions = {
    monitor,
    searchForGame,
  };

  const result = game as T & NewGameAddedProps;
  result.addOptions = addOptions;
  result.monitored = monitor !== 'none';
  result.qualityProfileId = qualityProfileId;
  result.minimumAvailability = minimumAvailability;
  result.rootFolderPath = rootFolderPath;
  result.tags = tags;

  return result;
}

export default getNewGame;
