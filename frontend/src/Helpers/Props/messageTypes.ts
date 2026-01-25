export const ERROR = 'error';
export const INFO = 'info';
export const SUCCESS = 'success';
export const WARNING = 'warning';

export type MessageType =
  | typeof ERROR
  | typeof INFO
  | typeof SUCCESS
  | typeof WARNING;

export const all: MessageType[] = [ERROR, INFO, SUCCESS, WARNING];
