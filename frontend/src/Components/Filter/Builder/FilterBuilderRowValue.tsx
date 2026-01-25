import { useCallback } from 'react';
import { FilterBuilderProp } from 'App/State/AppState';
import TagInput, { TagBase } from 'Components/Form/Tag/TagInput';
import { DeletedTag } from 'Components/Form/Tag/TagInputTag';
import {
  filterBuilderTypes,
  filterBuilderValueTypes,
  kinds,
} from 'Helpers/Props';
import convertToBytes from 'Utilities/Number/convertToBytes';
import formatBytes from 'Utilities/Number/formatBytes';
import {
  FilterBuilderRowOnChangeProps,
  FilterValue,
} from './FilterBuilderRowValueProps';
import FilterBuilderRowValueTag from './FilterBuilderRowValueTag';

export type { FilterValue } from './FilterBuilderRowValueProps';
export const NAME = 'value';

export interface Tag extends TagBase {
  id: boolean | number | string;
  name: string | number;
}

function getTagDisplayValue(
  value: boolean | number | string,
  selectedFilterBuilderProp: FilterBuilderProp<unknown>
): string | number {
  if (selectedFilterBuilderProp.valueType === filterBuilderValueTypes.BYTES) {
    return formatBytes(value as number);
  }

  return value as string | number;
}

function getValue(
  input: string,
  selectedFilterBuilderProp: FilterBuilderProp<unknown>
): number | string {
  if (selectedFilterBuilderProp.valueType === filterBuilderValueTypes.BYTES) {
    const match = input.match(/^(\d+)([kmgt](i?b)?)$/i);

    if (match && match.length > 1) {
      const [, value, unit] = input.match(/^(\d+)([kmgt](i?b)?)$/i)!;

      switch (unit.toLowerCase()) {
        case 'k':
          return convertToBytes(parseInt(value), 1, true);
        case 'm':
          return convertToBytes(parseInt(value), 2, true);
        case 'g':
          return convertToBytes(parseInt(value), 3, true);
        case 't':
          return convertToBytes(parseInt(value), 4, true);
        case 'kb':
          return convertToBytes(parseInt(value), 1, true);
        case 'mb':
          return convertToBytes(parseInt(value), 2, true);
        case 'gb':
          return convertToBytes(parseInt(value), 3, true);
        case 'tb':
          return convertToBytes(parseInt(value), 4, true);
        case 'kib':
          return convertToBytes(parseInt(value), 1, true);
        case 'mib':
          return convertToBytes(parseInt(value), 2, true);
        case 'gib':
          return convertToBytes(parseInt(value), 3, true);
        case 'tib':
          return convertToBytes(parseInt(value), 4, true);
        default:
          return parseInt(value);
      }
    }
  }

  if (selectedFilterBuilderProp.type === filterBuilderTypes.NUMBER) {
    const numberFractionDigits =
      (
        selectedFilterBuilderProp as FilterBuilderProp<unknown> & {
          numberFractionDigits?: number;
        }
      ).numberFractionDigits ?? 0;

    return Number(Number(input).toFixed(numberFractionDigits));
  }

  return input;
}

interface FilterBuilderRowValueProps {
  filterValue: FilterValue;
  selectedFilterBuilderProp: FilterBuilderProp<unknown>;
  tagList: Tag[];
  onChange: (payload: FilterBuilderRowOnChangeProps) => void;
}

function FilterBuilderRowValue({
  filterValue = [],
  selectedFilterBuilderProp,
  tagList,
  onChange,
}: FilterBuilderRowValueProps) {
  const onTagAdd = useCallback(
    (tag: Tag) => {
      let value: boolean | string | number = tag.id;

      if (value == null) {
        value = getValue(String(tag.name), selectedFilterBuilderProp);
      }

      onChange({
        name: NAME,
        value: [...filterValue, value],
      });
    },
    [filterValue, selectedFilterBuilderProp, onChange]
  );

  const onTagDelete = useCallback(
    ({ index }: DeletedTag<Tag>) => {
      const value = filterValue.filter(
        (_: boolean | string | number, i: number) => i !== index
      );

      onChange({
        name: NAME,
        value,
      });
    },
    [filterValue, onChange]
  );

  const hasItems = !!tagList.length;

  const tags: Tag[] = filterValue.map((id: boolean | string | number) => {
    if (hasItems) {
      const tag = tagList.find((t) => t.id === id);

      return {
        id,
        name: tag?.name ?? '',
      };
    }

    return {
      id,
      name: getTagDisplayValue(id, selectedFilterBuilderProp),
    };
  });

  return (
    <TagInput
      name={NAME}
      tags={tags}
      tagList={tagList}
      allowNew={!tagList.length}
      kind={kinds.DEFAULT}
      delimiters={['Tab', 'Enter']}
      minQueryLength={0}
      tagComponent={FilterBuilderRowValueTag}
      onTagAdd={onTagAdd}
      onTagDelete={onTagDelete}
    />
  );
}

export default FilterBuilderRowValue;
