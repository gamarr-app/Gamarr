import { createSelector, Selector } from 'reselect';
import AppState from 'App/State/AppState';
import ImportList from 'typings/ImportList';

function createImportListSelector(): Selector<AppState, ImportList[]> {
  return createSelector(
    (state: AppState) => state.settings.importLists.items,
    (lists) => {
      return lists;
    }
  );
}

export default createImportListSelector;
