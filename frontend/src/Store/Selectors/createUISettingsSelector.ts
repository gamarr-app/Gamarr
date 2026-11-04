import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  DEFAULT_LONG_DATE_FORMAT,
  DEFAULT_SHORT_DATE_FORMAT,
  DEFAULT_TIME_FORMAT,
} from 'Utilities/Date/defaultFormats';

function createUISettingsSelector() {
  return createSelector(
    (state: AppState) => state.settings.ui,
    (ui) => {
      const item = ui.item;

      // The item is `{}` until the fetch lands, and an older/partial payload
      // can lack a key; make sure the date formats are always strings.
      return {
        ...item,
        shortDateFormat: item.shortDateFormat || DEFAULT_SHORT_DATE_FORMAT,
        longDateFormat: item.longDateFormat || DEFAULT_LONG_DATE_FORMAT,
        timeFormat: item.timeFormat || DEFAULT_TIME_FORMAT,
      };
    }
  );
}

export default createUISettingsSelector;
