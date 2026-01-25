import { createAction } from 'redux-actions';
import { handleThunks } from 'Store/thunks';
import createHandleActions from './Creators/createHandleActions';
import autoTaggings from './Settings/autoTaggings';
import autoTaggingSpecifications from './Settings/autoTaggingSpecifications';
import customFormats from './Settings/customFormats';
import customFormatSpecifications from './Settings/customFormatSpecifications';
import delayProfiles from './Settings/delayProfiles';
import downloadClientOptions from './Settings/downloadClientOptions';
import downloadClients from './Settings/downloadClients';
import general from './Settings/general';
import importListExclusions from './Settings/importListExclusions';
import importListOptions from './Settings/importListOptions';
import importLists from './Settings/importLists';
import indexerFlags from './Settings/indexerFlags';
import indexerOptions from './Settings/indexerOptions';
import indexers from './Settings/indexers';
import languages from './Settings/languages';
import mediaManagement from './Settings/mediaManagement';
import metadata from './Settings/metadata';
import metadataOptions from './Settings/metadataOptions';
import naming from './Settings/naming';
import namingExamples from './Settings/namingExamples';
import notifications from './Settings/notifications';
import qualityDefinitions from './Settings/qualityDefinitions';
import qualityProfiles from './Settings/qualityProfiles';
import releaseProfiles from './Settings/releaseProfiles';
import remotePathMappings from './Settings/remotePathMappings';
import ui from './Settings/ui';

export * from './Settings/autoTaggingSpecifications';
export * from './Settings/autoTaggings';
export * from './Settings/customFormatSpecifications.js';
export * from './Settings/customFormats';
export * from './Settings/delayProfiles';
export * from './Settings/downloadClients';
export * from './Settings/downloadClientOptions';
export * from './Settings/general';
export * from './Settings/importListOptions';
export * from './Settings/importLists';
export * from './Settings/importListExclusions';
export * from './Settings/indexerFlags';
export * from './Settings/indexerOptions';
export * from './Settings/indexers';
export * from './Settings/languages';
export * from './Settings/mediaManagement';
export * from './Settings/metadata';
export * from './Settings/metadataOptions';
export * from './Settings/naming';
export * from './Settings/namingExamples';
export * from './Settings/notifications';
export * from './Settings/qualityDefinitions';
export * from './Settings/qualityProfiles';
export * from './Settings/releaseProfiles';
export * from './Settings/remotePathMappings';
export * from './Settings/ui';

export const section = 'settings';

export interface SettingsState {
  advancedSettings: boolean;
  autoTaggingSpecifications: typeof autoTaggingSpecifications.defaultState;
  autoTaggings: typeof autoTaggings.defaultState;
  customFormatSpecifications: typeof customFormatSpecifications.defaultState;
  customFormats: typeof customFormats.defaultState;
  delayProfiles: typeof delayProfiles.defaultState;
  downloadClients: typeof downloadClients.defaultState;
  downloadClientOptions: typeof downloadClientOptions.defaultState;
  general: typeof general.defaultState;
  importLists: typeof importLists.defaultState;
  importListExclusions: typeof importListExclusions.defaultState;
  importListOptions: typeof importListOptions.defaultState;
  indexerFlags: typeof indexerFlags.defaultState;
  indexerOptions: typeof indexerOptions.defaultState;
  indexers: typeof indexers.defaultState;
  languages: typeof languages.defaultState;
  mediaManagement: typeof mediaManagement.defaultState;
  metadata: typeof metadata.defaultState;
  metadataOptions: typeof metadataOptions.defaultState;
  naming: typeof naming.defaultState;
  namingExamples: typeof namingExamples.defaultState;
  notifications: typeof notifications.defaultState;
  qualityDefinitions: typeof qualityDefinitions.defaultState;
  qualityProfiles: typeof qualityProfiles.defaultState;
  releaseProfiles: typeof releaseProfiles.defaultState;
  remotePathMappings: typeof remotePathMappings.defaultState;
  ui: typeof ui.defaultState;
}

export const defaultState: SettingsState = {
  advancedSettings: false,
  autoTaggingSpecifications: autoTaggingSpecifications.defaultState,
  autoTaggings: autoTaggings.defaultState,
  customFormatSpecifications: customFormatSpecifications.defaultState,
  customFormats: customFormats.defaultState,
  delayProfiles: delayProfiles.defaultState,
  downloadClients: downloadClients.defaultState,
  downloadClientOptions: downloadClientOptions.defaultState,
  general: general.defaultState,
  importLists: importLists.defaultState,
  importListExclusions: importListExclusions.defaultState,
  importListOptions: importListOptions.defaultState,
  indexerFlags: indexerFlags.defaultState,
  indexerOptions: indexerOptions.defaultState,
  indexers: indexers.defaultState,
  languages: languages.defaultState,
  mediaManagement: mediaManagement.defaultState,
  metadata: metadata.defaultState,
  metadataOptions: metadataOptions.defaultState,
  naming: naming.defaultState,
  namingExamples: namingExamples.defaultState,
  notifications: notifications.defaultState,
  qualityDefinitions: qualityDefinitions.defaultState,
  qualityProfiles: qualityProfiles.defaultState,
  releaseProfiles: releaseProfiles.defaultState,
  remotePathMappings: remotePathMappings.defaultState,
  ui: ui.defaultState,
};

export const persistState = [
  'settings.advancedSettings',
  'settings.importListExclusions.pageSize',
];

export const TOGGLE_ADVANCED_SETTINGS = 'settings/toggleAdvancedSettings';

export const toggleAdvancedSettings = createAction(TOGGLE_ADVANCED_SETTINGS);

export const actionHandlers = handleThunks({
  ...autoTaggingSpecifications.actionHandlers,
  ...autoTaggings.actionHandlers,
  ...customFormatSpecifications.actionHandlers,
  ...customFormats.actionHandlers,
  ...delayProfiles.actionHandlers,
  ...downloadClients.actionHandlers,
  ...downloadClientOptions.actionHandlers,
  ...general.actionHandlers,
  ...importLists.actionHandlers,
  ...importListExclusions.actionHandlers,
  ...importListOptions.actionHandlers,
  ...indexerFlags.actionHandlers,
  ...indexerOptions.actionHandlers,
  ...indexers.actionHandlers,
  ...languages.actionHandlers,
  ...mediaManagement.actionHandlers,
  ...metadata.actionHandlers,
  ...metadataOptions.actionHandlers,
  ...naming.actionHandlers,
  ...namingExamples.actionHandlers,
  ...notifications.actionHandlers,
  ...qualityDefinitions.actionHandlers,
  ...qualityProfiles.actionHandlers,
  ...releaseProfiles.actionHandlers,
  ...remotePathMappings.actionHandlers,
  ...ui.actionHandlers,
});

export const reducers = createHandleActions(
  {
    [TOGGLE_ADVANCED_SETTINGS]: (state: SettingsState) => {
      return Object.assign({}, state, {
        advancedSettings: !state.advancedSettings,
      });
    },

    ...autoTaggingSpecifications.reducers,
    ...autoTaggings.reducers,
    ...customFormatSpecifications.reducers,
    ...customFormats.reducers,
    ...delayProfiles.reducers,
    ...downloadClients.reducers,
    ...downloadClientOptions.reducers,
    ...general.reducers,
    ...importLists.reducers,
    ...importListExclusions.reducers,
    ...importListOptions.reducers,
    ...indexerFlags.reducers,
    ...indexerOptions.reducers,
    ...indexers.reducers,
    ...languages.reducers,
    ...mediaManagement.reducers,
    ...metadata.reducers,
    ...metadataOptions.reducers,
    ...naming.reducers,
    ...namingExamples.reducers,
    ...notifications.reducers,
    ...qualityDefinitions.reducers,
    ...qualityProfiles.reducers,
    ...releaseProfiles.reducers,
    ...remotePathMappings.reducers,
    ...ui.reducers,
  },
  defaultState,
  section
);
