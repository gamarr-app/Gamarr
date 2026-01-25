import { Command } from './types';

function isCommandExecuting(command: Command | null | undefined): boolean {
  if (!command) {
    return false;
  }

  return command.status === 'queued' || command.status === 'started';
}

export default isCommandExecuting;
