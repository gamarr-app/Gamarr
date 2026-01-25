import React, { Component } from 'react';
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

class DateFilterBuilderRowValue extends Component<DateFilterBuilderRowValueProps> {
  //
  // Lifecycle

  componentDidMount() {
    const { filterType, filterValue, onChange } = this.props;

    if (isInFilter(filterType) && isString(filterValue)) {
      onChange({
        name: NAME,
        value: {
          time: timeOptions[0].key,
          value: null,
        },
      });
    }
  }

  componentDidUpdate(prevProps: DateFilterBuilderRowValueProps) {
    const { filterType, filterValue, onChange } = this.props;

    if (prevProps.filterType === filterType) {
      return;
    }

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
  }

  //
  // Listeners

  onValueChange = ({ value }: InputChanged<string | number | null>) => {
    const { filterValue, onChange } = this.props;

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
  };

  onTimeChange = ({ value }: InputChanged<string>) => {
    const { filterValue, onChange } = this.props;

    onChange({
      name: NAME,
      value: {
        time: value,
        value: (filterValue as DateFilterValue).value,
      },
    });
  };

  //
  // Render

  render() {
    const { filterType, filterValue } = this.props;

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
            onChange={this.onValueChange}
          />

          <SelectInput
            className={styles.selectInput}
            name={NAME}
            value={dateFilterValue.time}
            values={timeOptions}
            onChange={this.onTimeChange}
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
        onChange={this.onValueChange}
      />
    );
  }
}

export default DateFilterBuilderRowValue;
