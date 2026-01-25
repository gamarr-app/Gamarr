import _ from 'lodash';
import { useMemo } from 'react';
import { FilterBuilderProp } from 'App/State/AppState';
import { filterBuilderTypes } from 'Helpers/Props';
import * as filterTypes from 'Helpers/Props/filterTypes';
import FilterBuilderRowValue, { Tag } from './FilterBuilderRowValue';
import {
  FilterBuilderRowOnChangeProps,
  FilterValue,
} from './FilterBuilderRowValueProps';

interface FilterBuilderRowValueConnectorProps {
  filterType?: string;
  filterValue: FilterValue;
  sectionItems: unknown[];
  selectedFilterBuilderProp: FilterBuilderProp<unknown> & {
    optionsSelector?: (items: unknown[]) => Tag[];
  };
  onChange: (payload: FilterBuilderRowOnChangeProps) => void;
}

function getTagList(
  filterType: string | undefined,
  sectionItems: unknown[],
  selectedFilterBuilderProp: FilterBuilderRowValueConnectorProps['selectedFilterBuilderProp']
): Tag[] {
  if (
    ((selectedFilterBuilderProp.type === filterBuilderTypes.NUMBER ||
      selectedFilterBuilderProp.type === filterBuilderTypes.STRING) &&
      filterType !== filterTypes.EQUAL &&
      filterType !== filterTypes.NOT_EQUAL) ||
    !selectedFilterBuilderProp.optionsSelector
  ) {
    return [];
  }

  let items: Tag[] = [];

  if (selectedFilterBuilderProp.optionsSelector) {
    items = selectedFilterBuilderProp.optionsSelector(sectionItems);
  } else {
    items = (sectionItems as Record<string, unknown>[])
      .reduce((acc: Tag[], item) => {
        const name = item[selectedFilterBuilderProp.name];

        if (name) {
          acc.push({
            id: name as string,
            name: name as string,
          });
        }

        return acc;
      }, [])
      .sort((a, b) =>
        String(a.name).localeCompare(String(b.name), undefined, {
          numeric: true,
        })
      );
  }

  return _.uniqBy(items, 'id');
}

function FilterBuilderRowValueConnector({
  filterType,
  filterValue,
  sectionItems,
  selectedFilterBuilderProp,
  onChange,
}: FilterBuilderRowValueConnectorProps) {
  const tagList = useMemo(
    () => getTagList(filterType, sectionItems, selectedFilterBuilderProp),
    [filterType, sectionItems, selectedFilterBuilderProp]
  );

  return (
    <FilterBuilderRowValue
      filterValue={filterValue}
      selectedFilterBuilderProp={selectedFilterBuilderProp}
      tagList={tagList}
      onChange={onChange}
    />
  );
}

export default FilterBuilderRowValueConnector;
