import { useCallback, useEffect, useRef } from 'react';
import NumberInput from 'Components/Form/NumberInput';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import {
  IN_LAST,
  IN_NEXT,
  NOT_IN_LAST,
  NOT_IN_NEXT,
} from 'Helpers/Props/filterTypes';
import { InputChanged } from 'typings/inputs';
import isString from 'Utilities/String/isString';
import translate from 'Utilities/String/translate';
import { NAME } from './FilterBuilderRowValue';
import styles from './DateFilterBuilderRowValue.css';

const timeOptions = [
  {
    key: 'seconds',
    get value() {
      return translate('Seconds');
    },
  },
  {
    key: 'minutes',
    get value() {
      return translate('Minutes');
    },
  },
  {
    key: 'hours',
    get value() {
      return translate('Hours');
    },
  },
  {
    key: 'days',
    get value() {
      return translate('Days');
    },
  },
  {
    key: 'weeks',
    get value() {
      return translate('Weeks');
    },
  },
  {
    key: 'months',
    get value() {
      return translate('Months');
    },
  },
];

function isInFilter(filterType: string | undefined) {
  return (
    filterType === IN_LAST ||
    filterType === NOT_IN_LAST ||
    filterType === IN_NEXT ||
    filterType === NOT_IN_NEXT
  );
}

interface DateFilterValue {
  time: string;
  value: number | null;
}

interface DateFilterBuilderRowValueProps {
  filterType?: string;
  filterValue: string | DateFilterValue;
  onChange: (payload: {
    name: string;
    value: string | DateFilterValue;
  }) => void;
}

function DateFilterBuilderRowValue(props: DateFilterBuilderRowValueProps) {
  const { filterType, filterValue, onChange } = props;

  const prevFilterTypeRef = useRef(filterType);
  const isInitialMount = useRef(true);

  // Handle mount and filterType changes
  useEffect(() => {
    if (isInitialMount.current) {
      // componentDidMount equivalent
      isInitialMount.current = false;
      if (isInFilter(filterType) && isString(filterValue)) {
        onChange({
          name: NAME,
          value: {
            time: timeOptions[0].key,
            value: null,
          },
        });
      }
      return;
    }

    // componentDidUpdate equivalent
    if (prevFilterTypeRef.current === filterType) {
      prevFilterTypeRef.current = filterType;
      return;
    }

    prevFilterTypeRef.current = filterType;

    if (isInFilter(filterType) && isString(filterValue)) {
      onChange({
        name: NAME,
        value: {
          time: timeOptions[0].key,
          value: null,
        },
      });
      return;
    }

    if (!isInFilter(filterType) && !isString(filterValue)) {
      onChange({
        name: NAME,
        value: '',
      });
    }
  }, [filterType, filterValue, onChange]);

  const onValueChange = useCallback(
    ({ value }: InputChanged<string | number | null>) => {
      let newValue: string | DateFilterValue = value as string;

      if (!isString(value)) {
        newValue = {
          time: (filterValue as DateFilterValue).time,
          value: value as number,
        };
      }

      onChange({
        name: NAME,
        value: newValue,
      });
    },
    [filterValue, onChange]
  );

  const onTimeChange = useCallback(
    ({ value }: InputChanged<string>) => {
      onChange({
        name: NAME,
        value: {
          time: value,
          value: (filterValue as DateFilterValue).value,
        },
      });
    },
    [filterValue, onChange]
  );

  if (
    (isInFilter(filterType) && isString(filterValue)) ||
    (!isInFilter(filterType) && !isString(filterValue))
  ) {
    return null;
  }

  if (isInFilter(filterType)) {
    const dateFilterValue = filterValue as DateFilterValue;

    return (
      <div className={styles.container}>
        <NumberInput
          className={styles.numberInput}
          name={NAME}
          value={dateFilterValue.value}
          onChange={onValueChange}
        />

        <SelectInput
          className={styles.selectInput}
          name={NAME}
          value={dateFilterValue.time}
          values={timeOptions}
          onChange={onTimeChange}
        />
      </div>
    );
  }

  return (
    <TextInput
      name={NAME}
      value={filterValue as string}
      type="date"
      placeholder="yyyy-mm-dd"
      onChange={onValueChange}
    />
  );
}

export default DateFilterBuilderRowValue;
