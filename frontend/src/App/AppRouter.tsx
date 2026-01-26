import { createBrowserRouter, Outlet } from 'react-router-dom';
import Blocklist from 'Activity/Blocklist/Blocklist';
import History from 'Activity/History/History';
import Queue from 'Activity/Queue/Queue';
import AddNewGameConnector from 'AddGame/AddNewGame/AddNewGameConnector';
import ImportGameConnector from 'AddGame/ImportGame/Import/ImportGameConnector';
import ImportGameSelectFolderConnector from 'AddGame/ImportGame/SelectFolder/ImportGameSelectFolderConnector';
import CalendarPage from 'Calendar/CalendarPage';
import CollectionConnector from 'Collection/CollectionConnector';
import NotFound from 'Components/NotFound';
import Page from 'Components/Page/Page';
import DiscoverGameConnector from 'DiscoverGame/DiscoverGameConnector';
import GameDetailsPage from 'Game/Details/GameDetailsPage';
import GameIndex from 'Game/Index/GameIndex';
import CustomFormatSettingsPage from 'Settings/CustomFormats/CustomFormatSettingsPage';
import DownloadClientSettings from 'Settings/DownloadClients/DownloadClientSettings';
import GeneralSettings from 'Settings/General/GeneralSettings';
import ImportListSettings from 'Settings/ImportLists/ImportListSettings';
import IndexerSettings from 'Settings/Indexers/IndexerSettings';
import MediaManagement from 'Settings/MediaManagement/MediaManagement';
import MetadataSettings from 'Settings/Metadata/MetadataSettings';
import NotificationSettings from 'Settings/Notifications/NotificationSettings';
import Profiles from 'Settings/Profiles/Profiles';
import Quality from 'Settings/Quality/Quality';
import Settings from 'Settings/Settings';
import TagSettings from 'Settings/Tags/TagSettings';
import UISettings from 'Settings/UI/UISettings';
import BackupsConnector from 'System/Backup/BackupsConnector';
import LogsTableConnector from 'System/Events/LogsTableConnector';
import AppLogFiles from 'System/Logs/App/AppLogFiles';
import UpdateLogFiles from 'System/Logs/Update/UpdateLogFiles';
import Status from 'System/Status/Status';
import Tasks from 'System/Tasks/Tasks';
import Updates from 'System/Updates/Updates';
import CutoffUnmet from 'Wanted/CutoffUnmet/CutoffUnmet';
import Missing from 'Wanted/Missing/Missing';

function PageLayout() {
  return (
    <Page>
      <Outlet />
    </Page>
  );
}

export const router = createBrowserRouter(
  [
    {
      element: <PageLayout />,
      children: [
        // Games
        { path: '/', element: <GameIndex /> },
        { path: '/add/new', element: <AddNewGameConnector /> },
        { path: '/collections', element: <CollectionConnector /> },
        { path: '/add/import', element: <ImportGameSelectFolderConnector /> },
        { path: '/add/import/:rootFolderId', element: <ImportGameConnector /> },
        { path: '/add/discover', element: <DiscoverGameConnector /> },
        { path: '/game/:titleSlug', element: <GameDetailsPage /> },

        // Calendar
        { path: '/calendar', element: <CalendarPage /> },

        // Activity
        { path: '/activity/history', element: <History /> },
        { path: '/activity/queue', element: <Queue /> },
        { path: '/activity/blocklist', element: <Blocklist /> },

        // Wanted
        { path: '/wanted/missing', element: <Missing /> },
        { path: '/wanted/cutoffunmet', element: <CutoffUnmet /> },

        // Settings
        { path: '/settings', element: <Settings /> },
        { path: '/settings/mediamanagement', element: <MediaManagement /> },
        { path: '/settings/profiles', element: <Profiles /> },
        { path: '/settings/quality', element: <Quality /> },
        {
          path: '/settings/customformats',
          element: <CustomFormatSettingsPage />,
        },
        { path: '/settings/indexers', element: <IndexerSettings /> },
        {
          path: '/settings/downloadclients',
          element: <DownloadClientSettings />,
        },
        { path: '/settings/importlists', element: <ImportListSettings /> },
        { path: '/settings/connect', element: <NotificationSettings /> },
        { path: '/settings/metadata', element: <MetadataSettings /> },
        { path: '/settings/tags', element: <TagSettings /> },
        { path: '/settings/general', element: <GeneralSettings /> },
        { path: '/settings/ui', element: <UISettings /> },

        // System
        { path: '/system/status', element: <Status /> },
        { path: '/system/tasks', element: <Tasks /> },
        { path: '/system/backup', element: <BackupsConnector /> },
        { path: '/system/updates', element: <Updates /> },
        { path: '/system/events', element: <LogsTableConnector /> },
        { path: '/system/logs/files', element: <AppLogFiles /> },
        { path: '/system/logs/files/update', element: <UpdateLogFiles /> },

        // Not Found
        { path: '*', element: <NotFound /> },
      ],
    },
  ],
  {
    basename: window.Gamarr.urlBase,
  }
);
