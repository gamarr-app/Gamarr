import moment from 'moment';

type DateInput = string | Date | moment.Moment | null | undefined;

function isSameWeek(date: DateInput): boolean {
  if (!date) {
    return false;
  }

  return moment(date).isSame(moment(), 'week');
}

export default isSameWeek;
