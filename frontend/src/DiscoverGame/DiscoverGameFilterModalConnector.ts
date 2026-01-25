import React from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setListGameFilter } from 'Store/Actions/discoverGameActions';

interface DiscoverGameItem {
  igdbId: number;
  title: string;
  [key: string]: unknown;
}

interface FilterBuilderProp {
  name: string;
  label: () => string;
  type: string;
  valueType?: string;
  optionsSelector?: (
    items: DiscoverGameItem[]
  ) => Array<{ id: string; name: string }>;
}

interface DiscoverGameState {
  items: DiscoverGameItem[];
  filterBuilderProps: FilterBuilderProp[];
}

interface AppState {
  discoverGame: DiscoverGameState;
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.discoverGame.items,
    (state: AppState) => state.discoverGame.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'discoverGame',
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setListGameFilter,
};

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(FilterModal as React.ComponentType<unknown>);
