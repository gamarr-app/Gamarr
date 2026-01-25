import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  cancelLookupGame,
  lookupUnsearchedGames,
} from 'Store/Actions/importGameActions';
import ImportGameFooter from './ImportGameFooter';

interface ImportGameItem {
  id: string;
  monitor?: string;
  qualityProfileId?: number;
  minimumAvailability?: string;
  isPopulated?: boolean;
  [key: string]: unknown;
}

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
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.addGame,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.importGame,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (_state: any, { selectedIds }: { selectedIds: string[] }) => selectedIds,
    (addGame, importGame, selectedIds) => {
      const {
        monitor: defaultMonitor,
        qualityProfileId: defaultQualityProfileId,
        minimumAvailability: defaultMinimumAvailability,
      } = addGame.defaults;

      const { isLookingUpGame, isImporting, items, importError } = importGame;

      const isMonitorMixed = isMixed(
        items as ImportGameItem[],
        selectedIds,
        defaultMonitor,
        'monitor'
      );
      const isQualityProfileIdMixed = isMixed(
        items as ImportGameItem[],
        selectedIds,
        defaultQualityProfileId,
        'qualityProfileId'
      );
      const isMinimumAvailabilityMixed = isMixed(
        items as ImportGameItem[],
        selectedIds,
        defaultMinimumAvailability,
        'minimumAvailability'
      );
      const hasUnsearchedItems =
        !isLookingUpGame &&
        (items as ImportGameItem[]).some((item) => !item.isPopulated);

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
