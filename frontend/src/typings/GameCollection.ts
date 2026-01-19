import ModelBase from 'App/ModelBase';
import Game, { Image, GameAvailability } from 'Game/Game';

interface GameCollection extends ModelBase {
  igdbId: number;
  sortTitle: string;
  title: string;
  overview: string;
  monitored: boolean;
  minimumAvailability: GameAvailability;
  qualityProfileId: number;
  rootFolderPath: string;
  searchOnAdd: boolean;
  images: Image[];
  games: Game[];
  missingGames: number;
  tags: number[];
  isSaving?: boolean;
}

export default GameCollection;
