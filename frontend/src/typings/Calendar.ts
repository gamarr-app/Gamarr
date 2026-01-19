import Game from 'Game/Game';

export type CalendarItem = Game;

export type CalendarEvent = CalendarItem;

export type CalendarStatus =
  | 'downloaded'
  | 'queue'
  | 'unmonitored'
  | 'missingMonitored'
  | 'missingUnmonitored'
  | 'continuing';
