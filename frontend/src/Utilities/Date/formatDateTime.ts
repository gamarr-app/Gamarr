import moment from 'moment';
import translate from 'Utilities/String/translate';
import { DEFAULT_LONG_DATE_FORMAT } from './defaultFormats';
import formatTime from './formatTime';
import isToday from './isToday';
import isTomorrow from './isTomorrow';
import isYesterday from './isYesterday';

interface FormatDateTimeOptions {
  includeSeconds?: boolean;
  includeRelativeDay?: boolean;
}

function getRelativeDay(date: string, includeRelativeDate: boolean): string {
  if (!includeRelativeDate) {
    return '';
  }

  if (isYesterday(date)) {
    return translate('Yesterday');
  }

  if (isToday(date)) {
    return translate('Today');
  }

  if (isTomorrow(date)) {
    return translate('Tomorrow');
  }

  return '';
}

function formatDateTime(
  date: string | undefined | null,
  dateFormat: string | undefined | null,
  timeFormat: string | undefined | null,
  {
    includeSeconds = false,
    includeRelativeDay = false,
  }: FormatDateTimeOptions = {}
): string {
  if (!date) {
    return '';
  }

  const relativeDay = getRelativeDay(date, includeRelativeDay);
  const formattedDate = moment(date).format(dateFormat || DEFAULT_LONG_DATE_FORMAT);
  const formattedTime = formatTime(date, timeFormat, {
    includeMinuteZero: true,
    includeSeconds,
  });

  if (relativeDay) {
    return translate('FormatDateTimeRelative', {
      relativeDay,
      formattedDate,
      formattedTime,
    });
  }
  return translate('FormatDateTime', { formattedDate, formattedTime });
}

export default formatDateTime;
