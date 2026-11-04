import moment from 'moment';
import { DEFAULT_TIME_FORMAT } from './defaultFormats';

interface FormatTimeOptions {
  includeMinuteZero?: boolean;
  includeSeconds?: boolean;
}

function formatTime(
  date: string | undefined | null,
  timeFormat: string | undefined | null,
  { includeMinuteZero = false, includeSeconds = false }: FormatTimeOptions = {}
): string {
  if (!date) {
    return '';
  }

  const time = moment(date);
  let format = timeFormat || DEFAULT_TIME_FORMAT;

  if (includeSeconds) {
    format = format.replace(/\(?:mm\)?/, ':mm:ss');
  } else if (includeMinuteZero || time.minute() !== 0) {
    format = format.replace('(:mm)', ':mm');
  } else {
    format = format.replace('(:mm)', '');
  }

  return time.format(format);
}

export default formatTime;
