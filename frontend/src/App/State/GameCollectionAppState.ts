import AppSectionState, {
  AppSectionFilterState,
  AppSectionSaveState,
  Error,
} from 'App/State/AppSectionState';
import GameCollection from 'typings/GameCollection';

interface GameCollectionAppState
  extends AppSectionState<GameCollection>,
    AppSectionFilterState<GameCollection>,
    AppSectionSaveState {
  itemMap: Record<number, number>;

  isAdding: boolean;
  addError: Error;

  pendingChanges: Partial<GameCollection>;
}

export default GameCollectionAppState;
