import ModelBase from 'App/ModelBase';
import { Image } from 'Game/Game';

export type GameCreditType = 'cast' | 'crew';

interface GameCredit extends ModelBase {
  personIgdbId: number;
  personName: string;
  images: Image[];
  type: GameCreditType;
  department: string;
  job: string;
  character: string;
  order: number;
}

export default GameCredit;
