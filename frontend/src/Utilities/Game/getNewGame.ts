import Game, { GameAvailability, GameMonitor } from 'Game/Game';

interface NewGamePayload {
  rootFolderPath: string;
  monitor: GameMonitor;
  qualityProfileId: number;
  minimumAvailability: GameAvailability;
  tags: number[];
  searchForGame?: boolean;
}

function getNewGame(game: Game, payload: NewGamePayload) {
  const {
    rootFolderPath,
    monitor,
    qualityProfileId,
    minimumAvailability,
    tags,
    searchForGame = false,
  } = payload;

  const addOptions = {
    monitor,
    searchForGame,
  };

  game.addOptions = addOptions;
  game.monitored = monitor !== 'none';
  game.qualityProfileId = qualityProfileId;
  game.minimumAvailability = minimumAvailability;
  game.rootFolderPath = rootFolderPath;
  game.tags = tags;

  return game;
}

export default getNewGame;
