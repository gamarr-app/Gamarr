import _ from 'lodash';
import { SelectedState } from './areAllSelected';

interface SelectAllResult {
  allSelected: boolean;
  allUnselected: boolean;
  lastToggled: null;
  selectedState: SelectedState;
}

function selectAll(
  selectedState: SelectedState,
  selected: boolean
): SelectAllResult {
  const newSelectedState = _.reduce(
    Object.keys(selectedState),
    (result: SelectedState, item: string) => {
      result[item] = selected;
      return result;
    },
    {}
  );

  return {
    allSelected: selected,
    allUnselected: !selected,
    lastToggled: null,
    selectedState: newSelectedState,
  };
}

export default selectAll;
