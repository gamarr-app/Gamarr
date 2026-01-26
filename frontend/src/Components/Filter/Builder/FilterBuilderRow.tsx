import React, { ComponentType, useCallback, useEffect, useRef } from 'react';
import { FilterBuilderProp } from 'App/State/AppState';
import SelectInput from 'Components/Form/SelectInput';
import IconButton from 'Components/Link/IconButton';
import {
  filterBuilderTypes,
  filterBuilderValueTypes,
  icons,
} from 'Helpers/Props';
import sortByProp from 'Utilities/Array/sortByProp';
import BoolFilterBuilderRowValue from './BoolFilterBuilderRowValue';
import DateFilterBuilderRowValue from './DateFilterBuilderRowValue';
import FilterBuilderRowValueConnector from './FilterBuilderRowValueConnector';
import GameFilterBuilderRowValue from './GameFilterBuilderRowValue';
import HistoryEventTypeFilterBuilderRowValue from './HistoryEventTypeFilterBuilderRowValue';
import ImportListFilterBuilderRowValueConnector from './ImportListFilterBuilderRowValueConnector';
import IndexerFilterBuilderRowValueConnector from './IndexerFilterBuilderRowValueConnector';
import LanguageFilterBuilderRowValue from './LanguageFilterBuilderRowValue';
import MinimumAvailabilityFilterBuilderRowValue from './MinimumAvailabilityFilterBuilderRowValue';
import ProtocolFilterBuilderRowValue from './ProtocolFilterBuilderRowValue';
import QualityFilterBuilderRowValueConnector from './QualityFilterBuilderRowValueConnector';
import QualityProfileFilterBuilderRowValue from './QualityProfileFilterBuilderRowValue';
import QueueStatusFilterBuilderRowValue from './QueueStatusFilterBuilderRowValue';
import ReleaseStatusFilterBuilderRowValue from './ReleaseStatusFilterBuilderRowValue';
import TagFilterBuilderRowValueConnector from './TagFilterBuilderRowValueConnector';
import styles from './FilterBuilderRow.css';

interface FilterTypeOption {
  key: string;
  value: () => string;
}

interface PossibleFilterTypes {
  [key: string]: FilterTypeOption[];
}

function getselectedFilterBuilderProp(
  filterBuilderProps: FilterBuilderProp<unknown>[],
  name: string
): FilterBuilderProp<unknown> | undefined {
  return filterBuilderProps.find((a) => {
    return a.name === name;
  });
}

function getFilterTypeOptions(
  filterBuilderProps: FilterBuilderProp<unknown>[],
  filterKey: string | undefined
): FilterTypeOption[] {
  if (!filterKey) {
    return [];
  }

  const selectedFilterBuilderProp = getselectedFilterBuilderProp(
    filterBuilderProps,
    filterKey
  );

  if (!selectedFilterBuilderProp) {
    return [];
  }

  return (filterBuilderTypes.possibleFilterTypes as PossibleFilterTypes)[
    selectedFilterBuilderProp.type
  ];
}

function getDefaultFilterType(
  selectedFilterBuilderProp: FilterBuilderProp<unknown>
): string {
  return (filterBuilderTypes.possibleFilterTypes as PossibleFilterTypes)[
    selectedFilterBuilderProp.type
  ][0].key;
}

function getDefaultFilterValue(
  selectedFilterBuilderProp: FilterBuilderProp<unknown>
): string | unknown[] {
  if (selectedFilterBuilderProp.type === filterBuilderTypes.DATE) {
    return '';
  }

  return [];
}

function getRowValueConnector(
  selectedFilterBuilderProp: FilterBuilderProp<unknown> | undefined
): ComponentType<unknown> {
  if (!selectedFilterBuilderProp) {
    return FilterBuilderRowValueConnector as ComponentType<unknown>;
  }

  const valueType = selectedFilterBuilderProp.valueType;

  switch (valueType) {
    case filterBuilderValueTypes.BOOL:
      return BoolFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.DATE:
      return DateFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.HISTORY_EVENT_TYPE:
      return HistoryEventTypeFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.INDEXER:
      return IndexerFilterBuilderRowValueConnector as ComponentType<unknown>;

    case filterBuilderValueTypes.LANGUAGE:
      return LanguageFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.PROTOCOL:
      return ProtocolFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.QUALITY:
      return QualityFilterBuilderRowValueConnector as ComponentType<unknown>;

    case filterBuilderValueTypes.QUALITY_PROFILE:
      return QualityProfileFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.QUEUE_STATUS:
      return QueueStatusFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.GAME:
      return GameFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.RELEASE_STATUS:
      return ReleaseStatusFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.MINIMUM_AVAILABILITY:
      return MinimumAvailabilityFilterBuilderRowValue as ComponentType<unknown>;

    case filterBuilderValueTypes.TAG:
      return TagFilterBuilderRowValueConnector as ComponentType<unknown>;

    case filterBuilderValueTypes.IMPORTLIST:
      return ImportListFilterBuilderRowValueConnector as ComponentType<unknown>;

    default:
      return FilterBuilderRowValueConnector as ComponentType<unknown>;
  }
}

type FilterValueType =
  | string
  | number
  | unknown[]
  | Record<string, unknown>
  | undefined;

interface Filter {
  key: string;
  value: FilterValueType;
  type: string;
}

interface FilterBuilderRowProps {
  index: number;
  filterKey?: string;
  filterValue?: FilterValueType;
  filterType?: string;
  filterCount: number;
  filterBuilderProps: FilterBuilderProp<unknown>[];
  sectionItems: unknown[];
  onFilterChange: (index: number, filter: Filter) => void;
  onAddPress: (index: number) => void;
  onRemovePress: (index: number) => void;
}

function FilterBuilderRow(props: FilterBuilderRowProps) {
  const {
    index,
    filterKey,
    filterValue,
    filterType,
    filterCount,
    filterBuilderProps,
    sectionItems,
    onFilterChange,
    onAddPress,
    onRemovePress,
  } = props;

  const selectedFilterBuilderPropRef = useRef<
    FilterBuilderProp<unknown> | undefined
  >(
    filterKey ? filterBuilderProps.find((a) => a.name === filterKey) : undefined
  );

  // Initialize filter on mount if no filterKey
  useEffect(() => {
    if (filterKey) {
      selectedFilterBuilderPropRef.current = filterBuilderProps.find(
        (a) => a.name === filterKey
      );
      return;
    }

    const selectedFilterBuilderProp = filterBuilderProps[0];

    const filter: Filter = {
      key: selectedFilterBuilderProp.name,
      value: getDefaultFilterValue(selectedFilterBuilderProp),
      type: getDefaultFilterType(selectedFilterBuilderProp),
    };

    selectedFilterBuilderPropRef.current = selectedFilterBuilderProp;
    onFilterChange(index, filter);
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleFilterKeyChange = useCallback(
    ({ value: key }: { value: string }) => {
      const selectedFilterBuilderProp = getselectedFilterBuilderProp(
        filterBuilderProps,
        key
      )!;
      const type = getDefaultFilterType(selectedFilterBuilderProp);

      const filter: Filter = {
        key,
        value: getDefaultFilterValue(selectedFilterBuilderProp),
        type,
      };

      selectedFilterBuilderPropRef.current = selectedFilterBuilderProp;
      onFilterChange(index, filter);
    },
    [filterBuilderProps, index, onFilterChange]
  );

  const handleFilterChange = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      const filter: Filter = {
        key: filterKey!,
        value: filterValue,
        type: filterType!,
      };

      if (name === 'key') {
        filter.key = value as string;
      } else if (name === 'value') {
        filter.value = value as FilterValueType;
      } else if (name === 'type') {
        filter.type = value as string;
      }

      onFilterChange(index, filter);
    },
    [filterKey, filterValue, filterType, index, onFilterChange]
  );

  const handleAddPress = useCallback(() => {
    onAddPress(index);
  }, [index, onAddPress]);

  const handleRemovePress = useCallback(() => {
    onRemovePress(index);
  }, [index, onRemovePress]);

  const selectedFilterBuilderProp = selectedFilterBuilderPropRef.current;

  const keyOptions = filterBuilderProps
    .map((availablePropFilter) => {
      const { name, label } = availablePropFilter;

      return {
        key: name,
        value: typeof label === 'function' ? (label as () => string)() : label,
      };
    })
    .sort(sortByProp('value'));

  const ValueComponent = getRowValueConnector(selectedFilterBuilderProp);

  return (
    <div className={styles.filterRow}>
      <div className={styles.inputContainer}>
        {filterKey && (
          <SelectInput
            name="key"
            value={filterKey}
            values={keyOptions}
            onChange={handleFilterKeyChange}
          />
        )}
      </div>

      <div className={styles.inputContainer}>
        {filterType && (
          <SelectInput
            name="type"
            value={filterType}
            values={getFilterTypeOptions(filterBuilderProps, filterKey)}
            onChange={handleFilterChange}
          />
        )}
      </div>

      <div className={styles.valueInputContainer}>
        {filterValue != null &&
          !!selectedFilterBuilderProp &&
          React.createElement(
            ValueComponent as React.ComponentType<{
              filterType?: string;
              filterValue: FilterValueType;
              selectedFilterBuilderProp: FilterBuilderProp<unknown>;
              sectionItems: unknown[];
              onChange: (payload: { name: string; value: unknown }) => void;
            }>,
            {
              filterType,
              filterValue,
              selectedFilterBuilderProp,
              sectionItems,
              onChange: handleFilterChange,
            }
          )}
      </div>

      <div className={styles.actionsContainer}>
        <IconButton
          name={icons.SUBTRACT}
          isDisabled={filterCount === 1}
          onPress={handleRemovePress}
        />

        <IconButton name={icons.ADD} onPress={handleAddPress} />
      </div>
    </div>
  );
}

export default FilterBuilderRow;
