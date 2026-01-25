import areAllSelected, {
  AllSelectedResult,
  SelectedState,
} from './areAllSelected';

interface Item {
  id: number | string;
}

interface State {
  selectedState: SelectedState;
}

interface RemoveOldSelectedStateResult extends AllSelectedResult {
  selectedState: SelectedState;
}

export default function removeOldSelectedState(
  state: State,
  prevItems: Item[]
): RemoveOldSelectedStateResult {
  const selectedState: SelectedState = {
    ...state.selectedState,
  };

  prevItems.forEach((item) => {
    delete selectedState[item.id];
  });

  return {
    ...areAllSelected(selectedState),
    selectedState,
  };
}
