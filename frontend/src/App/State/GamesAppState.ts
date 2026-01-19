import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import Column from 'Components/Table/Column';
import { SortDirection } from 'Helpers/Props/sortDirections';
import Game from 'Game/Game';
import { Filter, FilterBuilderProp } from './AppState';

export interface GameIndexAppState {
  sortKey: string;
  sortDirection: SortDirection;
  secondarySortKey: string;
  secondarySortDirection: SortDirection;
  view: string;

  posterOptions: {
    detailedProgressBar: boolean;
    size: string;
    showTitle: boolean;
    showMonitored: boolean;
    showQualityProfile: boolean;
    showCinemaRelease: boolean;
    showDigitalRelease: boolean;
    showPhysicalRelease: boolean;
    showReleaseDate: boolean;
    showIgdbRating: boolean;
    showImdbRating: boolean;
    showRottenTomatoesRating: boolean;
    showTraktRating: boolean;
    showTags: boolean;
    showSearchAction: boolean;
  };

  overviewOptions: {
    detailedProgressBar: boolean;
    size: string;
    showMonitored: boolean;
    showStudio: boolean;
    showQualityProfile: boolean;
    showAdded: boolean;
    showPath: boolean;
    showSizeOnDisk: boolean;
    showTags: boolean;
    showSearchAction: boolean;
  };

  tableOptions: {
    showSearchAction: boolean;
  };

  selectedFilterKey: string;
  filterBuilderProps: FilterBuilderProp<Game>[];
  filters: Filter[];
  columns: Column[];
}

interface GamesAppState
  extends AppSectionState<Game>,
    AppSectionDeleteState,
    AppSectionSaveState {
  itemMap: Record<number, number>;

  deleteOptions: {
    addImportExclusion: boolean;
  };

  pendingChanges: Partial<Game>;
}

export default GamesAppState;
