import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
  TableAppSectionState,
} from 'App/State/AppSectionState';
import { GameFile } from 'GameFile/GameFile';

interface GameFilesAppState
  extends AppSectionState<GameFile>,
    AppSectionDeleteState,
    AppSectionSaveState,
    TableAppSectionState {}

export default GameFilesAppState;
