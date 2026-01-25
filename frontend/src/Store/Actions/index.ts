import * as addGame from './addGameActions';
import * as app from './appActions';
import * as blocklist from './blocklistActions';
import * as calendar from './calendarActions';
import * as captcha from './captchaActions';
import * as commands from './commandActions';
import * as customFilters from './customFilterActions';
import * as discoverGame from './discoverGameActions';
import * as extraFiles from './extraFileActions';
import * as games from './gameActions';
import * as gameBlocklist from './gameBlocklistActions';
import * as gameCollections from './gameCollectionActions';
import * as gameFiles from './gameFileActions';
import * as gameHistory from './gameHistoryActions';
import * as gameIndex from './gameIndexActions';
import * as history from './historyActions';
import * as importGame from './importGameActions';
import * as interactiveImportActions from './interactiveImportActions';
import * as oAuth from './oAuthActions';
import * as organizePreview from './organizePreviewActions';
import * as parse from './parseActions';
import * as paths from './pathActions';
import * as providerOptions from './providerOptionActions';
import * as queue from './queueActions';
import * as releases from './releaseActions';
import * as rootFolders from './rootFolderActions';
import * as settings from './settingsActions';
import * as system from './systemActions';
import * as tags from './tagActions';
import * as wanted from './wantedActions';

export default [
  addGame,
  app,
  blocklist,
  calendar,
  captcha,
  commands,
  customFilters,
  discoverGame,
  gameFiles,
  extraFiles,
  history,
  importGame,
  interactiveImportActions,
  oAuth,
  organizePreview,
  parse,
  paths,
  providerOptions,
  queue,
  releases,
  rootFolders,
  games,
  gameBlocklist,
  gameCollections,
  gameHistory,
  gameIndex,
  settings,
  system,
  tags,
  wanted,
];
