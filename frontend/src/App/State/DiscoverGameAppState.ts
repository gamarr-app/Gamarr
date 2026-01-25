import AppSectionState from 'App/State/AppSectionState';
import Column from 'Components/Table/Column';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { Filter, FilterBuilderProp } from './AppState';

export interface DiscoverGameItem {
  id: number;
  igdbId: number;
  title: string;
  sortTitle: string;
  year?: number;
  studio?: string;
  status: string;
  genres: string[];
  collection?: { title?: string; igdbId?: number };
  originalLanguage?: { name: string };
  inCinemas?: string;
  physicalRelease?: string;
  digitalRelease?: string;
  ratings?: {
    igdb?: { value: number };
    metacritic?: { value: number };
  };
  isExcluded: boolean;
  isExisting: boolean;
  lists: unknown[];
}

export interface DiscoverGameOptions {
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
}

export interface DiscoverGamePosterOptions {
  size: string;
  showTitle: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
}

export interface DiscoverGameOverviewOptions {
  size: string;
  showYear: boolean;
  showStudio: boolean;
  showGenres: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  showCertification: boolean;
}

interface DiscoverGameAppState extends AppSectionState<DiscoverGameItem> {
  isAdding: boolean;
  isAdded: boolean;
  addError: unknown;
  sortKey: string;
  sortDirection: SortDirection;
  secondarySortKey: string;
  secondarySortDirection: SortDirection;
  view: string;
  options: DiscoverGameOptions;
  posterOptions: DiscoverGamePosterOptions;
  overviewOptions: DiscoverGameOverviewOptions;
  tableOptions: Record<string, unknown>;
  selectedFilterKey: string;
  filterBuilderProps: FilterBuilderProp<DiscoverGameItem>[];
  filters: Filter[];
  columns: Column[];
}

export default DiscoverGameAppState;
