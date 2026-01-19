import React from 'react';
import { CustomFilter, Filter } from 'App/State/AppState';
import GameCollectionFilterModal from 'Collection/GameCollectionFilterModal';
import FilterMenu from 'Components/Menu/FilterMenu';

interface GameCollectionFilterMenuProps {
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  isDisabled: boolean;
  onFilterSelect: (filter: number | string) => void;
}

function GameCollectionFilterMenu({
  selectedFilterKey,
  filters,
  customFilters,
  isDisabled,
  onFilterSelect,
}: GameCollectionFilterMenuProps) {
  return (
    <FilterMenu
      alignMenu="right"
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      filterModalConnectorComponent={GameCollectionFilterModal}
      onFilterSelect={onFilterSelect}
    />
  );
}

export default GameCollectionFilterMenu;
