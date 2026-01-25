import { RouterState } from 'connected-react-router';
import AddGameAppState from './AddGameAppState';
import { Error } from './AppSectionState';
import BlocklistAppState from './BlocklistAppState';
import CalendarAppState from './CalendarAppState';
import CaptchaAppState from './CaptchaAppState';
import CommandAppState from './CommandAppState';
import CustomFiltersAppState from './CustomFiltersAppState';
import DiscoverGameAppState from './DiscoverGameAppState';
import ExtraFilesAppState from './ExtraFilesAppState';
import GameBlocklistAppState from './GameBlocklistAppState';
import GameCollectionAppState from './GameCollectionAppState';
import GameFilesAppState from './GameFilesAppState';
import GamesAppState, { GameIndexAppState } from './GamesAppState';
import HistoryAppState, { GameHistoryAppState } from './HistoryAppState';
import ImportGameAppState from './ImportGameAppState';
import InteractiveImportAppState from './InteractiveImportAppState';
import MessagesAppState from './MessagesAppState';
import OAuthAppState from './OAuthAppState';
import OrganizePreviewAppState from './OrganizePreviewAppState';
import ParseAppState from './ParseAppState';
import PathsAppState from './PathsAppState';
import ProviderOptionsAppState from './ProviderOptionsAppState';
import QueueAppState from './QueueAppState';
import ReleasesAppState from './ReleasesAppState';
import RootFolderAppState from './RootFolderAppState';
import SettingsAppState from './SettingsAppState';
import SystemAppState from './SystemAppState';
import TagsAppState from './TagsAppState';
import WantedAppState from './WantedAppState';

interface FilterBuilderPropOption {
  id: string;
  name: string;
}

export interface FilterBuilderProp<T> {
  name: string;
  label: string;
  type: string;
  valueType?: string;
  optionsSelector?: (items: T[]) => FilterBuilderPropOption[];
}

export interface PropertyFilter {
  key: string;
  value: boolean | string | number | string[] | number[];
  type: string;
}

export interface Filter {
  key: string;
  label: string | (() => string);
  filters: PropertyFilter[];
}

export interface CustomFilter {
  id: number;
  type: string;
  label: string;
  filters: PropertyFilter[];
}

export interface AppSectionState {
  isUpdated: boolean;
  isConnected: boolean;
  isDisconnected: boolean;
  isReconnecting: boolean;
  isRestarting: boolean;
  isSidebarVisible: boolean;
  version: string;
  prevVersion?: string;
  dimensions: {
    isSmallScreen: boolean;
    isLargeScreen: boolean;
    width: number;
    height: number;
  };
  translations: {
    error?: Error;
    isPopulated: boolean;
  };
  messages: MessagesAppState;
}

interface AppState {
  addGame: AddGameAppState;
  app: AppSectionState;
  blocklist: BlocklistAppState;
  calendar: CalendarAppState;
  captcha: CaptchaAppState;
  commands: CommandAppState;
  customFilters: CustomFiltersAppState;
  discoverGame: DiscoverGameAppState;
  extraFiles: ExtraFilesAppState;
  history: HistoryAppState;
  importGame: ImportGameAppState;
  interactiveImport: InteractiveImportAppState;
  gameBlocklist: GameBlocklistAppState;
  gameCollections: GameCollectionAppState;
  gameFiles: GameFilesAppState;
  gameHistory: GameHistoryAppState;
  gameIndex: GameIndexAppState;
  games: GamesAppState;
  oAuth: OAuthAppState;
  organizePreview: OrganizePreviewAppState;
  parse: ParseAppState;
  paths: PathsAppState;
  providerOptions: ProviderOptionsAppState;
  queue: QueueAppState;
  releases: ReleasesAppState;
  rootFolders: RootFolderAppState;
  router: RouterState;
  settings: SettingsAppState;
  system: SystemAppState;
  tags: TagsAppState;
  wanted: WantedAppState;
}

export default AppState;
