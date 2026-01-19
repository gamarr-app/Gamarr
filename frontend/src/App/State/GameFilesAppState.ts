import AppSectionState, {
  AppSectionDeleteState,
} from 'App/State/AppSectionState';
import { GameFile } from 'GameFile/GameFile';

interface GameFilesAppState
  extends AppSectionState<GameFile>,
    AppSectionDeleteState {}

export default GameFilesAppState;
