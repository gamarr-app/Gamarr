import * as signalR from '@microsoft/signalr';
import { Component } from 'react';
import { connect, ConnectedProps } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { setAppValue, setVersion } from 'Store/Actions/appActions';
import { removeItem, update, updateItem } from 'Store/Actions/baseActions';
import {
  fetchCommands,
  finishCommand,
  updateCommand,
} from 'Store/Actions/commandActions';
import { fetchGames } from 'Store/Actions/gameActions';
import { fetchQueue, fetchQueueDetails } from 'Store/Actions/queueActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import { fetchQualityDefinitions } from 'Store/Actions/settingsActions';
import { fetchHealth } from 'Store/Actions/systemActions';
import { fetchTagDetails, fetchTags } from 'Store/Actions/tagActions';
import { repopulatePage } from 'Utilities/pagePopulator';
import titleCase from 'Utilities/String/titleCase';

// SignalR message types
interface SignalRMessage {
  name: string;
  body: MessageBody;
}

interface MessageBody {
  action?: string;
  resource?: Record<string, unknown>;
  version?: string;
}

interface ResourceWithId {
  id: number;
  [key: string]: unknown;
}

// SignalR logger interface
interface Logger {
  minimumLogLevel: number;
  cleanse: (message: string) => string;
  log: (logLevel: number, message: string) => void;
}

function getHandlerName(name: string): string {
  name = titleCase(name);
  name = name.replace('/', '');

  return `handle${name}`;
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.app.isReconnecting,
    (state: AppState) => state.app.isDisconnected,
    (state: AppState) => state.queue.paged.isPopulated,
    (isReconnecting, isDisconnected, isQueuePopulated) => {
      return {
        isReconnecting,
        isDisconnected,
        isQueuePopulated,
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchFetchCommands: fetchCommands,
  dispatchUpdateCommand: updateCommand,
  dispatchFinishCommand: finishCommand,
  dispatchSetAppValue: setAppValue,
  dispatchSetVersion: setVersion,
  dispatchUpdate: update,
  dispatchUpdateItem: updateItem,
  dispatchRemoveItem: removeItem,
  dispatchFetchHealth: fetchHealth,
  dispatchFetchQualityDefinitions: fetchQualityDefinitions,
  dispatchFetchQueue: fetchQueue,
  dispatchFetchQueueDetails: fetchQueueDetails,
  dispatchFetchRootFolders: fetchRootFolders,
  dispatchFetchGames: fetchGames,
  dispatchFetchTags: fetchTags,
  dispatchFetchTagDetails: fetchTagDetails,
};

const connector = connect(createMapStateToProps, mapDispatchToProps);

type PropsFromRedux = ConnectedProps<typeof connector>;

function createLogger(minimumLogLevel: number): Logger {
  return {
    minimumLogLevel,
    cleanse(message: string): string {
      const apikey = new RegExp(
        `access_token=${encodeURIComponent(window.Gamarr.apiKey)}`,
        'g'
      );
      return message.replace(apikey, 'access_token=(removed)');
    },
    log(logLevel: number, message: string): void {
      // see https://github.com/aspnet/AspNetCore/blob/21c9e2cc954c10719878839cd3f766aca5f57b34/src/SignalR/clients/ts/signalr/src/Utils.ts#L147
      if (logLevel >= this.minimumLogLevel) {
        switch (logLevel) {
          case signalR.LogLevel.Critical:
          case signalR.LogLevel.Error:
            console.error(
              `[signalR] ${signalR.LogLevel[logLevel]}: ${this.cleanse(
                message
              )}`
            );
            break;
          case signalR.LogLevel.Warning:
            console.warn(
              `[signalR] ${signalR.LogLevel[logLevel]}: ${this.cleanse(
                message
              )}`
            );
            break;
          case signalR.LogLevel.Information:
            console.info(
              `[signalR] ${signalR.LogLevel[logLevel]}: ${this.cleanse(
                message
              )}`
            );
            break;
          default:
            // console.debug only goes to attached debuggers in Node, so we use console.log for Trace and Debug
            console.log(
              `[signalR] ${signalR.LogLevel[logLevel]}: ${this.cleanse(
                message
              )}`
            );
            break;
        }
      }
    },
  };
}

class SignalRConnector extends Component<PropsFromRedux> {
  private connection: signalR.HubConnection | null = null;

  //
  // Lifecycle

  componentDidMount() {
    console.log('[signalR] starting');

    const url = `${window.Gamarr.urlBase}/signalr/messages`;

    this.connection = new signalR.HubConnectionBuilder()
      .configureLogging(createLogger(signalR.LogLevel.Information))
      .withUrl(
        `${url}?access_token=${encodeURIComponent(window.Gamarr.apiKey)}`
      )
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds > 180000) {
            this.props.dispatchSetAppValue({ isDisconnected: true });
          }
          return Math.min(retryContext.previousRetryCount, 10) * 1000;
        },
      })
      .build();

    this.connection.onreconnecting(this.onReconnecting);
    this.connection.onreconnected(this.onReconnected);
    this.connection.onclose(this.onClose);

    this.connection.on('receiveMessage', this.onReceiveMessage);

    this.connection.start().then(this.onStart, this.onStartFail);
  }

  componentWillUnmount() {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
    }
  }

  //
  // Control
  handleMessage = (message: SignalRMessage) => {
    const { name, body } = message;

    const handlerName = getHandlerName(name) as keyof SignalRConnector;
    const handler = this[handlerName] as
      | ((body: MessageBody) => void)
      | undefined;

    if (handler && typeof handler === 'function') {
      handler.call(this, body);
      return;
    }

    console.error(`signalR: Unable to find handler for ${name}`);
  };

  handleCalendar = (body: MessageBody) => {
    if (body.action === 'updated') {
      this.props.dispatchUpdateItem({
        section: 'calendar',
        updateOnly: true,
        ...body.resource,
      });
    }
  };

  handleCommand = (body: MessageBody) => {
    if (body.action === 'sync') {
      this.props.dispatchFetchCommands();
      return;
    }

    const resource = body.resource;
    const status = resource?.status as string | undefined;

    // Both successful and failed commands need to be
    // completed, otherwise they spin until they time out.

    if (status === 'completed' || status === 'failed') {
      this.props.dispatchFinishCommand(resource as Record<string, unknown>);
    } else {
      this.props.dispatchUpdateCommand(resource as Record<string, unknown>);
    }
  };

  handleGamefile = (body: MessageBody) => {
    const section = 'gameFiles';

    if (body.action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...body.resource });

      // Repopulate the page to handle recently imported file
      repopulatePage('gameFileUpdated');
    } else if (body.action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (body.resource as ResourceWithId).id,
      });

      repopulatePage('gameFileDeleted');
    }
  };

  handleDownloadclient = ({
    action,
    resource,
  }: {
    action?: string;
    resource?: Record<string, unknown>;
  }) => {
    const section = 'settings.downloadClients';

    if (action === 'created' || action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...resource });
    } else if (action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (resource as ResourceWithId).id,
      });
    }
  };

  handleHealth = () => {
    this.props.dispatchFetchHealth();
  };

  handleImportlist = ({
    action,
    resource,
  }: {
    action?: string;
    resource?: Record<string, unknown>;
  }) => {
    const section = 'settings.importLists';

    if (action === 'created' || action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...resource });
    } else if (action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (resource as ResourceWithId).id,
      });
    }
  };

  handleIndexer = ({
    action,
    resource,
  }: {
    action?: string;
    resource?: Record<string, unknown>;
  }) => {
    const section = 'settings.indexers';

    if (action === 'created' || action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...resource });
    } else if (action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (resource as ResourceWithId).id,
      });
    }
  };

  handleMetadata = ({
    action,
    resource,
  }: {
    action?: string;
    resource?: Record<string, unknown>;
  }) => {
    const section = 'settings.metadata';

    if (action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...resource });
    }
  };

  handleNotification = ({
    action,
    resource,
  }: {
    action?: string;
    resource?: Record<string, unknown>;
  }) => {
    const section = 'settings.notifications';

    if (action === 'created' || action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...resource });
    } else if (action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (resource as ResourceWithId).id,
      });
    }
  };

  handleGame = (body: MessageBody) => {
    const action = body.action;
    const section = 'games';

    if (action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...body.resource });

      repopulatePage('gameUpdated');
    } else if (action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (body.resource as ResourceWithId).id,
      });
    }
  };

  handleCollection = (body: MessageBody) => {
    const action = body.action;
    const section = 'gameCollections';

    if (action === 'updated') {
      this.props.dispatchUpdateItem({ section, ...body.resource });
    } else if (action === 'deleted') {
      this.props.dispatchRemoveItem({
        section,
        id: (body.resource as ResourceWithId).id,
      });
    }
  };

  handleQualitydefinition = () => {
    this.props.dispatchFetchQualityDefinitions();
  };

  handleQueue = () => {
    if (this.props.isQueuePopulated) {
      this.props.dispatchFetchQueue();
    }
  };

  handleQueueDetails = () => {
    this.props.dispatchFetchQueueDetails();
  };

  handleQueueStatus = (body: MessageBody) => {
    this.props.dispatchUpdate({ section: 'queue.status', data: body.resource });
  };

  handleVersion = (body: MessageBody) => {
    const version = body.version;

    if (version) {
      this.props.dispatchSetVersion({ version });
    }
  };

  handleWantedCutoff = (body: MessageBody) => {
    if (body.action === 'updated') {
      this.props.dispatchUpdateItem({
        section: 'wanted.cutoffUnmet',
        updateOnly: true,
        ...body.resource,
      });
    }
  };

  handleWantedMissing = (body: MessageBody) => {
    if (body.action === 'updated') {
      this.props.dispatchUpdateItem({
        section: 'wanted.missing',
        updateOnly: true,
        ...body.resource,
      });
    }
  };

  handleSystemTask = () => {
    this.props.dispatchFetchCommands();
  };

  handleRootfolder = () => {
    this.props.dispatchFetchRootFolders();
  };

  handleTag = (body: MessageBody) => {
    if (body.action === 'sync') {
      this.props.dispatchFetchTags();
      this.props.dispatchFetchTagDetails();
      return;
    }
  };

  //
  // Listeners

  onStartFail = (error: Error) => {
    console.error('[signalR] failed to connect');
    console.error(error);

    this.props.dispatchSetAppValue({
      isConnected: false,
      isReconnecting: false,
      isDisconnected: false,
      isRestarting: false,
    });
  };

  onStart = () => {
    console.debug('[signalR] connected');

    this.props.dispatchSetAppValue({
      isConnected: true,
      isReconnecting: false,
      isDisconnected: false,
      isRestarting: false,
    });
  };

  onReconnecting = () => {
    this.props.dispatchSetAppValue({ isReconnecting: true });
  };

  onReconnected = () => {
    const { dispatchFetchCommands, dispatchFetchGames, dispatchSetAppValue } =
      this.props;

    dispatchSetAppValue({
      isConnected: true,
      isReconnecting: false,
      isDisconnected: false,
      isRestarting: false,
    });

    // Repopulate the page (if a repopulator is set) to ensure things
    // are in sync after reconnecting.
    dispatchFetchGames();
    dispatchFetchCommands();
    repopulatePage();
  };

  onClose = () => {
    console.debug('[signalR] connection closed');
  };

  onReceiveMessage = (message: SignalRMessage) => {
    console.debug('[signalR] received', message.name, message.body);

    this.handleMessage(message);
  };

  //
  // Render

  render() {
    return null;
  }
}

export default connector(SignalRConnector);
