import _ from 'lodash';
import isSameCommand, { CommandBody } from './isSameCommand';
import { Command } from './types';

function findCommand(
  commands: Command[],
  options: CommandBody
): Command | undefined {
  return _.findLast(commands, (command) => {
    // Cast to CommandBody since isSameCommand only needs name and index access
    return isSameCommand(command.body as unknown as CommandBody, options);
  });
}

export default findCommand;
