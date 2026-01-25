export interface SelectedState {
  [key: string]: boolean;
}

export interface AllSelectedResult {
  allSelected: boolean;
  allUnselected: boolean;
}

export default function areAllSelected(
  selectedState: SelectedState
): AllSelectedResult {
  let allSelected = true;
  let allUnselected = true;

  Object.keys(selectedState).forEach((key) => {
    if (selectedState[key]) {
      allUnselected = false;
    } else {
      allSelected = false;
    }
  });

  return {
    allSelected,
    allUnselected,
  };
}
