import moment from 'moment';

function formatDate(
  date: string | undefined | null,
  dateFormat: string
): string {
  if (!date) {
    return '';
  }

  return moment(date).format(dateFormat);
}

export default formatDate;
