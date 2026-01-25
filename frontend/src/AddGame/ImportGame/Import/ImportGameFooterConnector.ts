import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ImportGameItem } from 'App/State/ImportGameAppState';
import {
  cancelLookupGame,
  lookupUnsearchedGames,
} from 'Store/Actions/importGameActions';
import ImportGameFooter from './ImportGameFooter';

function isMixed(
  items: ImportGameItem[],
  selectedIds: string[],
  defaultValue: unknown,
  key: keyof ImportGameItem
): boolean {
  return _.some(items, (game) => {
    return selectedIds.indexOf(game.id) > -1 && game[key] !== defaultValue;
  });
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.addGame,
    (state: AppState) => state.importGame,
    (_state: AppState, { selectedIds }: { selectedIds: string[] }) =>
      selectedIds,
    (addGame, importGame, selectedIds) => {
      const {
        monitor: defaultMonitor,
        qualityProfileId: defaultQualityProfileId,
        minimumAvailability: defaultMinimumAvailability,
      } = addGame.defaults;

      const { isLookingUpGame, isImporting, items, importError } = importGame;

      const isMonitorMixed = isMixed(
        items,
        selectedIds,
        defaultMonitor,
        'monitor'
      );
      const isQualityProfileIdMixed = isMixed(
        items,
        selectedIds,
        defaultQualityProfileId,
        'qualityProfileId'
      );
      const isMinimumAvailabilityMixed = isMixed(
        items,
        selectedIds,
        defaultMinimumAvailability,
        'minimumAvailability'
      );
      const hasUnsearchedItems =
        !isLookingUpGame && items.some((item) => !item.isPopulated);

      return {
        selectedCount: selectedIds.length,
        isLookingUpGame,
        isImporting,
        defaultMonitor,
        defaultQualityProfileId,
        defaultMinimumAvailability,
        isMonitorMixed,
        isQualityProfileIdMixed,
        isMinimumAvailabilityMixed,
        importError,
        hasUnsearchedItems,
      };
    }
  );
}

const mapDispatchToProps = {
  onLookupPress: lookupUnsearchedGames,
  onCancelLookupPress: cancelLookupGame,
};

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameFooter);
