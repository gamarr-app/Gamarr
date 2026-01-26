import _ from 'lodash';
import isSameCommand, { CommandBody } from './isSameCommand';
import { Command } from './types';

function findCommand(
  commands: Command[],
  options: CommandBody
): Command | undefined {
  return _.findLast(commands, (command) => {
    // Create a CommandBody-compatible object from command.body
    // This is needed because isSameCommand requires an index signature for dynamic property access
    const body: CommandBody = { name: command.body.name };
    Object.keys(command.body).forEach((key) => {
      body[key] = command.body[key as keyof typeof command.body];
    });
    return isSameCommand(body, options);
  });
}

export default findCommand;
