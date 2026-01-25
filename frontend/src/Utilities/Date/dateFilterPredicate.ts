import moment from 'moment';
import * as filterTypes from 'Helpers/Props/filterTypes';
import isAfter from 'Utilities/Date/isAfter';
import isBefore from 'Utilities/Date/isBefore';

interface TimeFilterValue {
  time: string;
  value: number;
}

type FilterValue = string | TimeFilterValue;

export default function dateFilterPredicate(
  itemValue: string | undefined | null,
  filterValue: FilterValue,
  type: string
): boolean {
  if (!itemValue) {
    return false;
  }

  switch (type) {
    case filterTypes.LESS_THAN:
      return moment(itemValue).isBefore(filterValue as string);

    case filterTypes.GREATER_THAN:
      return moment(itemValue).isAfter(filterValue as string);

    case filterTypes.IN_LAST:
      return (
        isAfter(itemValue, {
          [(filterValue as TimeFilterValue).time]:
            (filterValue as TimeFilterValue).value * -1,
        }) && isBefore(itemValue)
      );

    case filterTypes.NOT_IN_LAST:
      return isBefore(itemValue, {
        [(filterValue as TimeFilterValue).time]:
          (filterValue as TimeFilterValue).value * -1,
      });

    case filterTypes.IN_NEXT:
      return (
        isAfter(itemValue) &&
        isBefore(itemValue, {
          [(filterValue as TimeFilterValue).time]: (
            filterValue as TimeFilterValue
          ).value,
        })
      );

    case filterTypes.NOT_IN_NEXT:
      return isAfter(itemValue, {
        [(filterValue as TimeFilterValue).time]: (
          filterValue as TimeFilterValue
        ).value,
      });

    default:
      return false;
  }
}
