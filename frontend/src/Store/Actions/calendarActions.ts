import _ from 'lodash';
import moment, { Moment } from 'moment';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import * as calendarViews from 'Calendar/calendarViews';
import * as commandNames from 'Commands/commandNames';
import {
  filterBuilderTypes,
  filterBuilderValueTypes,
  filterTypes,
} from 'Helpers/Props';
import { AppDispatch, createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';
import translate from 'Utilities/String/translate';
import { set, update } from './baseActions';
import { executeCommandHelper } from './commandActions';
import createHandleActions from './Creators/createHandleActions';
import createClearReducer from './Creators/Reducers/createClearReducer';

//
// Variables

export const section = 'calendar';

const viewRanges: Record<string, string> = {
  [calendarViews.DAY]: 'day',
  [calendarViews.WEEK]: 'week',
  [calendarViews.MONTH]: 'month',
  [calendarViews.FORECAST]: 'day',
};

//
// State

interface CalendarOptions {
  showGameInformation: boolean;
  showCinemaRelease: boolean;
  showDigitalRelease: boolean;
  showPhysicalRelease: boolean;
  showCutoffUnmetIcon: boolean;
  fullColorEvents: boolean;
}

interface FilterBuilderProp {
  name: string;
  label: () => string;
  type: string;
  valueType: string;
}

export interface CalendarState {
  isFetching: boolean;
  isPopulated: boolean;
  start: string | null;
  end: string | null;
  time?: string;
  dates: string[];
  dayCount: number;
  view: string;
  error: unknown;
  items: unknown[];
  searchMissingCommandId: number | null;
  options: CalendarOptions;
  selectedFilterKey: string;
  filters: Filter[];
  filterBuilderProps: FilterBuilderProp[];
}

// eslint-disable-next-line init-declarations
declare const window: Window & {
  innerWidth: number;
};

export const defaultState: CalendarState = {
  isFetching: false,
  isPopulated: false,
  start: null,
  end: null,
  dates: [],
  dayCount: 7,
  view: window.innerWidth > 768 ? 'month' : 'day',
  error: null,
  items: [],
  searchMissingCommandId: null,

  options: {
    showGameInformation: true,
    showCinemaRelease: true,
    showDigitalRelease: true,
    showPhysicalRelease: true,
    showCutoffUnmetIcon: false,
    fullColorEvents: false,
  },

  selectedFilterKey: 'monitored',

  filters: [
    {
      key: 'all',
      label: () => translate('All'),
      filters: [
        {
          key: 'unmonitored',
          value: true,
          type: filterTypes.EQUAL,
        },
      ],
    },
    {
      key: 'monitored',
      label: () => translate('MonitoredOnly'),
      filters: [
        {
          key: 'unmonitored',
          value: false,
          type: filterTypes.EQUAL,
        },
      ],
    },
  ],

  filterBuilderProps: [
    {
      name: 'unmonitored',
      label: () => translate('IncludeUnmonitored'),
      type: filterBuilderTypes.EQUAL,
      valueType: filterBuilderValueTypes.BOOL,
    },
    {
      name: 'tags',
      label: () => translate('Tags'),
      type: filterBuilderTypes.CONTAINS,
      valueType: filterBuilderValueTypes.TAG,
    },
  ],
};

export const persistState = [
  'calendar.view',
  'calendar.selectedFilterKey',
  'calendar.options',
  'gameIndex.customFilters',
];

//
// Actions Types

export const FETCH_CALENDAR = 'calendar/fetchCalendar';
export const SET_CALENDAR_DAYS_COUNT = 'calendar/setCalendarDaysCount';
export const SET_CALENDAR_FILTER = 'calendar/setCalendarFilter';
export const SET_CALENDAR_VIEW = 'calendar/setCalendarView';
export const GOTO_CALENDAR_TODAY = 'calendar/gotoCalendarToday';
export const GOTO_CALENDAR_NEXT_RANGE = 'calendar/gotoCalendarNextRange';
export const CLEAR_CALENDAR = 'calendar/clearCalendar';
export const SET_CALENDAR_OPTION = 'calendar/setCalendarOption';
export const SEARCH_MISSING = 'calendar/searchMissing';
export const GOTO_CALENDAR_PREVIOUS_RANGE =
  'calendar/gotoCalendarPreviousRange';

//
// Helpers

interface DatesResult {
  start: string;
  end: string;
  time: string;
  dates: string[];
}

interface PopulatableRange {
  start: string;
  end: string;
}

function getDays(start: string, end: string): string[] {
  const startTime = moment(start);
  const endTime = moment(end);
  const difference = endTime.diff(startTime, 'days');

  // Difference is one less than the number of days we need to account for.
  return _.times(difference + 1, (i) => {
    return startTime.clone().add(i, 'days').toISOString();
  });
}

function getDates(
  time: Moment,
  view: string,
  firstDayOfWeek: number,
  dayCount: number
): DatesResult {
  const weekName = firstDayOfWeek === 0 ? 'week' : 'isoWeek';

  let start = time.clone().startOf('day');
  let end = time.clone().endOf('day');

  if (view === calendarViews.WEEK) {
    start = time.clone().startOf(weekName as moment.unitOfTime.StartOf);
    end = time.clone().endOf(weekName as moment.unitOfTime.StartOf);
  }

  if (view === calendarViews.FORECAST) {
    start = time.clone().subtract(1, 'day').startOf('day');
    end = time
      .clone()
      .add(dayCount - 2, 'days')
      .endOf('day');
  }

  if (view === calendarViews.MONTH) {
    start = time
      .clone()
      .startOf('month')
      .startOf(weekName as moment.unitOfTime.StartOf);
    end = time
      .clone()
      .endOf('month')
      .endOf(weekName as moment.unitOfTime.StartOf);
  }

  if (view === calendarViews.AGENDA) {
    start = time.clone().subtract(1, 'day').startOf('day');
    end = time.clone().add(1, 'month').endOf('day');
  }

  return {
    start: start.toISOString(),
    end: end.toISOString(),
    time: time.toISOString(),
    dates: getDays(start.toISOString(), end.toISOString()),
  };
}

function getPopulatableRange(
  startDate: string,
  endDate: string,
  view: string
): PopulatableRange {
  switch (view) {
    case calendarViews.DAY:
      return {
        start: moment(startDate).subtract(1, 'day').toISOString(),
        end: moment(endDate).add(1, 'day').toISOString(),
      };
    case calendarViews.WEEK:
    case calendarViews.FORECAST:
      return {
        start: moment(startDate).subtract(1, 'week').toISOString(),
        end: moment(endDate).add(1, 'week').toISOString(),
      };
    default:
      return {
        start: startDate,
        end: endDate,
      };
  }
}

function isRangePopulated(
  start: string,
  _end: string,
  state: CalendarState
): boolean {
  const { start: currentStart, end: currentEnd, view: currentView } = state;

  if (!currentStart || !currentEnd) {
    return false;
  }

  const { start: currentPopulatedStart, end: currentPopulatedEnd } =
    getPopulatableRange(currentStart, currentEnd, currentView);

  if (
    moment(start).isAfter(currentPopulatedStart) &&
    moment(start).isBefore(currentPopulatedEnd)
  ) {
    return true;
  }

  return false;
}

function getCustomFilters(state: AppState, type: string): CustomFilter[] {
  return state.customFilters.items.filter(
    (customFilter) => customFilter.type === type
  );
}

//
// Action Creators

interface FetchCalendarPayload {
  time?: string;
  view?: string;
}

interface SetCalendarDaysCountPayload {
  dayCount: number;
}

interface SetCalendarFilterPayload {
  selectedFilterKey: string;
}

interface SetCalendarViewPayload {
  view: string;
}

interface SearchMissingPayload {
  gameIds: number[];
}

export const fetchCalendar = createThunk(FETCH_CALENDAR);
export const setCalendarDaysCount = createThunk(SET_CALENDAR_DAYS_COUNT);
export const setCalendarFilter = createThunk(SET_CALENDAR_FILTER);
export const setCalendarView = createThunk(SET_CALENDAR_VIEW);
export const gotoCalendarToday = createThunk(GOTO_CALENDAR_TODAY);
export const gotoCalendarPreviousRange = createThunk(
  GOTO_CALENDAR_PREVIOUS_RANGE
);
export const gotoCalendarNextRange = createThunk(GOTO_CALENDAR_NEXT_RANGE);
export const clearCalendar = createAction(CLEAR_CALENDAR);
export const setCalendarOption =
  createAction<Partial<CalendarOptions>>(SET_CALENDAR_OPTION);
export const searchMissing = createThunk(SEARCH_MISSING);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_CALENDAR]: function (
    getState: () => AppState,
    payload: FetchCalendarPayload,
    dispatch: AppDispatch
  ) {
    const state = getState();
    const calendar = state.calendar as unknown as CalendarState;
    const customFilters = getCustomFilters(state, section);
    const selectedFilters = findSelectedFilters(
      calendar.selectedFilterKey,
      calendar.filters,
      customFilters
    );

    const { time = calendar.time, view = calendar.view } = payload;

    const dayCount = (state.calendar as unknown as CalendarState).dayCount;
    const settingsState = state.settings as unknown as {
      ui: { item: { firstDayOfWeek: number } };
    };
    const dates = getDates(
      moment(time),
      view,
      settingsState.ui.item.firstDayOfWeek,
      dayCount
    );
    const { start, end } = getPopulatableRange(dates.start, dates.end, view);
    const isPrePopulated = isRangePopulated(
      start,
      end,
      state.calendar as unknown as CalendarState
    );

    const basesAttrs = {
      section,
      isFetching: true,
    };

    const attrs = isPrePopulated
      ? {
          view,
          ...basesAttrs,
          ...dates,
        }
      : basesAttrs;

    dispatch(set(attrs));

    const requestParams: Record<string, unknown> = {
      start,
      end,
    };

    selectedFilters.forEach((selectedFilter) => {
      if (selectedFilter.key === 'unmonitored') {
        requestParams.unmonitored = selectedFilter.value === true;
      }

      if (selectedFilter.key === 'tags') {
        requestParams.tags = (selectedFilter.value as string[]).join(',');
      }
    });

    requestParams.unmonitored = requestParams.unmonitored ?? false;

    const promise = createAjaxRequest({
      url: '/calendar',
      data: requestParams,
    }).request;

    promise.done((data: unknown[]) => {
      dispatch(
        batchActions([
          update({ section, data }),

          set({
            section,
            view,
            ...dates,
            isFetching: false,
            isPopulated: true,
            error: null,
          }),
        ])
      );
    });

    promise.fail((xhr: unknown) => {
      dispatch(
        set({
          section,
          isFetching: false,
          isPopulated: false,
          error: xhr,
        })
      );
    });
  },

  [SET_CALENDAR_DAYS_COUNT]: function (
    getState: () => AppState,
    payload: SetCalendarDaysCountPayload,
    dispatch: AppDispatch
  ) {
    if (
      payload.dayCount ===
      (getState().calendar as unknown as CalendarState).dayCount
    ) {
      return;
    }

    dispatch(
      set({
        section,
        dayCount: payload.dayCount,
      })
    );

    const state = getState();
    const { time, view } = state.calendar as unknown as CalendarState;

    dispatch(fetchCalendar({ time, view }));
  },

  [SET_CALENDAR_FILTER]: function (
    getState: () => AppState,
    payload: SetCalendarFilterPayload,
    dispatch: AppDispatch
  ) {
    dispatch(
      set({
        section,
        selectedFilterKey: payload.selectedFilterKey,
      })
    );

    const state = getState();
    const { time, view } = state.calendar as unknown as CalendarState;

    dispatch(fetchCalendar({ time, view }));
  },

  [SET_CALENDAR_VIEW]: function (
    getState: () => AppState,
    payload: SetCalendarViewPayload,
    dispatch: AppDispatch
  ) {
    const state = getState();
    const view = payload.view;
    const time =
      view === calendarViews.FORECAST || calendarViews.AGENDA
        ? moment()
        : (state.calendar as unknown as CalendarState).time;

    dispatch(fetchCalendar({ time: time?.toString(), view }));
  },

  [GOTO_CALENDAR_TODAY]: function (
    getState: () => AppState,
    _payload: unknown,
    dispatch: AppDispatch
  ) {
    const state = getState();
    const view = (state.calendar as unknown as CalendarState).view;
    const time = moment();

    dispatch(fetchCalendar({ time: time.toISOString(), view }));
  },

  [GOTO_CALENDAR_PREVIOUS_RANGE]: function (
    getState: () => AppState,
    _payload: unknown,
    dispatch: AppDispatch
  ) {
    const state = getState();

    const { view, dayCount } = state.calendar as unknown as CalendarState;

    const amount = view === calendarViews.FORECAST ? dayCount : 1;
    const time = moment(
      (state.calendar as unknown as CalendarState).time
    ).subtract(
      amount,
      viewRanges[view] as moment.unitOfTime.DurationConstructor
    );

    dispatch(fetchCalendar({ time: time.toISOString(), view }));
  },

  [GOTO_CALENDAR_NEXT_RANGE]: function (
    getState: () => AppState,
    _payload: unknown,
    dispatch: AppDispatch
  ) {
    const state = getState();

    const { view, dayCount } = state.calendar as unknown as CalendarState;

    const amount = view === calendarViews.FORECAST ? dayCount : 1;
    const time = moment((state.calendar as unknown as CalendarState).time).add(
      amount,
      viewRanges[view] as moment.unitOfTime.DurationConstructor
    );

    dispatch(fetchCalendar({ time: time.toISOString(), view }));
  },

  [SEARCH_MISSING]: function (
    _getState: () => AppState,
    payload: SearchMissingPayload,
    dispatch: AppDispatch
  ) {
    const { gameIds } = payload;

    const commandPayload = {
      name: commandNames.GAME_SEARCH,
      gameIds,
    };

    executeCommandHelper(commandPayload, dispatch)?.then(
      (data: { id: number }) => {
        dispatch(
          set({
            section,
            searchMissingCommandId: data.id,
          })
        );
      }
    );
  },
});

//
// Reducers

export const reducers = createHandleActions(
  {
    [CLEAR_CALENDAR]: createClearReducer(section, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: [],
    }),

    [SET_CALENDAR_OPTION]: function (
      state: CalendarState,
      { payload }: { payload: Partial<CalendarOptions> }
    ) {
      const options = state.options;

      return {
        ...state,
        options: {
          ...options,
          ...payload,
        },
      };
    },
  },
  defaultState,
  section
);
