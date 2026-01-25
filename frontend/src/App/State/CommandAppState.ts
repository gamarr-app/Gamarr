import AppSectionState from 'App/State/AppSectionState';
import Command from 'Commands/Command';

interface CommandHandler {
  name: string;
  handler: (payload: Command) => unknown;
}

export interface CommandAppState extends AppSectionState<Command> {
  handlers: Record<string, CommandHandler>;
}

export default CommandAppState;
