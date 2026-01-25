import moment from 'moment';

type DateInput = string | Date | moment.Moment | null | undefined;

function isInNextWeek(date: DateInput): boolean {
  if (!date) {
    return false;
  }
  const now = moment();
  return moment(date).isBetween(now, now.clone().add(6, 'days').endOf('day'));
}

export default isInNextWeek;
