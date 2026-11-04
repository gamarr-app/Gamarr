import moment from 'moment';
import { DEFAULT_SHORT_DATE_FORMAT } from './defaultFormats';

function formatDate(
  date: string | undefined | null,
  dateFormat: string | undefined | null
): string {
  if (!date) {
    return '';
  }

  return moment(date).format(dateFormat || DEFAULT_SHORT_DATE_FORMAT);
}

export default formatDate;
