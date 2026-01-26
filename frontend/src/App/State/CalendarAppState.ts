import AppSectionState, {
  AppSectionFilterState,
} from 'App/State/AppSectionState';
import { CalendarView } from 'Calendar/calendarViews';
import { CalendarItem } from 'typings/Calendar';

interface CalendarOptions {
  showGameInformation: boolean;
  showCinemaRelease: boolean;
  showDigitalRelease: boolean;
  showPhysicalRelease: boolean;
  showCutoffUnmetIcon: boolean;
  fullColorEvents: boolean;
}

interface CalendarAppState
  extends AppSectionState<CalendarItem>,
    AppSectionFilterState<CalendarItem> {
  searchMissingCommandId: number | null;
  start: string | null;
  end: string | null;
  dates: string[];
  dayCount: number;
  time: string;
  view: CalendarView;
  options: CalendarOptions;
}

export default CalendarAppState;
