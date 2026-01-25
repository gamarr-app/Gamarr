import { FilterBuilderProp } from 'App/State/AppState';

export type FilterValue = Array<boolean | string | number>;

export interface FilterBuilderRowOnChangeProps {
  name: string;
  value: unknown;
}

interface FilterBuilderRowValueProps {
  filterType?: string;
  filterValue: FilterValue;
  selectedFilterBuilderProp: FilterBuilderProp<unknown>;
  sectionItems?: unknown[];
  onChange: (payload: FilterBuilderRowOnChangeProps) => void;
}

export default FilterBuilderRowValueProps;
