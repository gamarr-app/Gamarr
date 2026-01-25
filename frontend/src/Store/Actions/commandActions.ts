import { Dispatch } from 'redux';
import { batchActions } from 'redux-batched-actions';
import AppState from 'App/State/AppState';
import { messageTypes } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import { isSameCommand } from 'Utilities/Command';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import { hideMessage, showMessage } from './appActions';
import { removeItem, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';

//
// Variables

export const section = 'commands';

let lastCommand: CommandPayload | null = null;
let lastCommandTimeout: ReturnType<typeof setTimeout> | null = null;
const removeCommandTimeoutIds: Record<
  number,
  ReturnType<typeof setTimeout>
> = {};
const commandFinishedCallbacks: Record<number, (payload: CommandData) => void> =
  {};

//
// State

interface CommandHandler {
  name: string;
  handler: (payload: CommandData) => unknown;
}

export interface CommandsState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: CommandData[];
  handlers: Record<string, CommandHandler>;
}

export const defaultState: CommandsState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],
  handlers: {},
};

//
// Actions Types

export const FETCH_COMMANDS = 'commands/fetchCommands';
export const EXECUTE_COMMAND = 'commands/executeCommand';
export const CANCEL_COMMAND = 'commands/cancelCommand';
export const ADD_COMMAND = 'commands/addCommand';
export const UPDATE_COMMAND = 'commands/updateCommand';
export const FINISH_COMMAND = 'commands/finishCommand';
export const REMOVE_COMMAND = 'commands/removeCommand';

//
// Action Creators

export const fetchCommands = createThunk(FETCH_COMMANDS);
export const executeCommand = createThunk(EXECUTE_COMMAND);
export const cancelCommand = createThunk(CANCEL_COMMAND);
export const addCommand = createThunk(ADD_COMMAND);
export const updateCommand = createThunk(UPDATE_COMMAND);
export const finishCommand = createThunk(FINISH_COMMAND);
export const removeCommand = createThunk(REMOVE_COMMAND);

//
// Helpers

interface CommandData {
  id: number;
  name: string;
  trigger?: string;
  message?: string;
  body?: {
    sendUpdatesToClient?: boolean;
    suppressMessages?: boolean;
  };
  status: string;
}

interface CommandPayload {
  name: string;
  commandFinished?: (payload: CommandData) => void;
  [key: string]: unknown;
}

function showCommandMessage(payload: CommandData, dispatch: Dispatch): void {
  const { id, name, trigger, message, body = {}, status } = payload;

  const { sendUpdatesToClient, suppressMessages } = body;

  if (!message || !body || !sendUpdatesToClient || suppressMessages) {
    return;
  }

  let type = messageTypes.INFO;
  let hideAfter = 0;

  if (status === 'completed') {
    type = messageTypes.SUCCESS;
    hideAfter = 4;
  } else if (status === 'failed') {
    type = messageTypes.ERROR;
    hideAfter = trigger === 'manual' ? 10 : 4;
  }

  dispatch(
    showMessage({
      id,
      name,
      message,
      type,
      hideAfter,
    })
  );
}

function scheduleRemoveCommand(command: CommandData, dispatch: Dispatch): void {
  const { id, status } = command;

  if (status === 'queued') {
    return;
  }

  const timeoutId = removeCommandTimeoutIds[id];

  if (timeoutId) {
    clearTimeout(timeoutId);
  }

  removeCommandTimeoutIds[id] = setTimeout(() => {
    dispatch(
      batchActions([
        removeCommand({ section: 'commands', id }),
        hideMessage({ id }),
      ])
    );

    delete removeCommandTimeoutIds[id];
  }, 60000 * 5);
}

export function executeCommandHelper(
  payload: CommandPayload,
  dispatch: Dispatch
): Promise<CommandData> | undefined {
  if (lastCommand && isSameCommand(lastCommand, payload)) {
    dispatch(
      showMessage({
        id: 'command-throttled',
        name: 'commandThrottled',
        message: translate('CommandThrottled'),
        type: messageTypes.WARNING,
        hideAfter: 5,
      })
    );

    return;
  }

  lastCommand = payload;

  // clear last command after 5 seconds.
  if (lastCommandTimeout) {
    clearTimeout(lastCommandTimeout);
  }

  lastCommandTimeout = setTimeout(() => {
    lastCommand = null;
  }, 5000);

  const { commandFinished, ...requestPayload } = payload;

  const promise = createAjaxRequest({
    url: '/command',
    method: 'POST',
    data: JSON.stringify(requestPayload),
    dataType: 'json',
  }).request;

  return promise.then((data: CommandData) => {
    if (commandFinished) {
      commandFinishedCallbacks[data.id] = commandFinished;
    }

    dispatch(addCommand(data));
    return data;
  });
}

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_COMMANDS]: createFetchHandler('commands', '/command'),

  [EXECUTE_COMMAND]: function (
    _getState: () => AppState,
    payload: CommandPayload,
    dispatch: Dispatch
  ) {
    executeCommandHelper(payload, dispatch);
  },

  [CANCEL_COMMAND]: createRemoveItemHandler(section, '/command'),

  [ADD_COMMAND]: function (
    _getState: () => AppState,
    payload: CommandData,
    dispatch: Dispatch
  ) {
    dispatch(updateItem({ section: 'commands', ...payload }));
  },

  [UPDATE_COMMAND]: function (
    _getState: () => AppState,
    payload: CommandData,
    dispatch: Dispatch
  ) {
    dispatch(updateItem({ section: 'commands', ...payload }));

    showCommandMessage(payload, dispatch);
    scheduleRemoveCommand(payload, dispatch);
  },

  [FINISH_COMMAND]: function (
    getState: () => AppState,
    payload: CommandData,
    dispatch: Dispatch
  ) {
    const state = getState();
    const handlers = (state.commands as CommandsState).handlers;

    Object.keys(handlers).forEach((key) => {
      const handler = handlers[key];

      if (handler.name === payload.name) {
        dispatch(
          handler.handler(payload) as unknown as Parameters<Dispatch>[0]
        );
      }
    });

    const commandFinished = commandFinishedCallbacks[payload.id];

    if (commandFinished) {
      commandFinished(payload);
    }

    delete commandFinishedCallbacks[payload.id];

    dispatch(updateItem({ section: 'commands', ...payload }));
    scheduleRemoveCommand(payload, dispatch);
    showCommandMessage(payload, dispatch);
  },

  [REMOVE_COMMAND]: function (
    _getState: () => AppState,
    payload: { id: number },
    dispatch: Dispatch
  ) {
    dispatch(removeItem({ section: 'commands', ...payload }));
  },
});

//
// Reducers

export const reducers = createHandleActions({}, defaultState, section);
