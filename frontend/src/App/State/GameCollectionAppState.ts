import AppSectionState, {
  AppSectionFilterState,
  AppSectionSaveState,
  Error,
} from 'App/State/AppSectionState';
import GameCollection from 'typings/GameCollection';

export interface CollectionOverviewOptions {
  detailedProgressBar: boolean;
  size: string;
  showDetails: boolean;
  showOverview: boolean;
  showPosters: boolean;
}

interface GameCollectionAppState
  extends AppSectionState<GameCollection>,
    AppSectionFilterState<GameCollection>,
    AppSectionSaveState {
  itemMap: Record<number, number>;

  isAdding: boolean;
  addError: Error;

  pendingChanges: Partial<GameCollection>;

  overviewOptions: CollectionOverviewOptions;
}

export default GameCollectionAppState;
