import moment, { DurationInputArg2 } from 'moment';

type DateInput = string | Date | moment.Moment | null | undefined;
type Offsets = Partial<Record<DurationInputArg2, number>>;

function isAfter(date: DateInput, offsets: Offsets = {}): boolean {
  if (!date) {
    return false;
  }

  const offsetTime = moment();

  Object.keys(offsets).forEach((key) => {
    offsetTime.add(offsets[key as DurationInputArg2], key as DurationInputArg2);
  });

  return moment(date).isAfter(offsetTime);
}

export default isAfter;
