import { Command } from './types';

function isCommandComplete(command: Command | null | undefined): boolean {
  if (!command) {
    return false;
  }

  return command.status === 'complete';
}

export default isCommandComplete;
