// Re-export the Command type from the main Commands module
import OriginalCommand from 'Commands/Command';

export type { CommandBody } from 'Commands/Command';

export type CommandStatus =
  | 'queued'
  | 'started'
  | 'complete'
  | 'failed'
  | 'aborted'
  | 'cancelled'
  | 'orphaned';

// Use the existing Command type to avoid type conflicts
export type Command = OriginalCommand;
