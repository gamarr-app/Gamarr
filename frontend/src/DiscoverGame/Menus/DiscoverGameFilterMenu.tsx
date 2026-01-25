import { CustomFilter, Filter } from 'App/State/AppState';
import FilterMenu from 'Components/Menu/FilterMenu';
import DiscoverGameFilterModalConnector from 'DiscoverGame/DiscoverGameFilterModalConnector';
import { align } from 'Helpers/Props';

interface DiscoverGameFilterMenuProps {
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  isDisabled: boolean;
  onFilterSelect: (filter: string | number) => void;
}

function DiscoverGameFilterMenu({
  selectedFilterKey,
  filters,
  customFilters,
  isDisabled,
  onFilterSelect,
}: DiscoverGameFilterMenuProps) {
  return (
    <FilterMenu
      alignMenu={align.RIGHT}
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      filterModalConnectorComponent={DiscoverGameFilterModalConnector}
      onFilterSelect={onFilterSelect}
    />
  );
}

export default DiscoverGameFilterMenu;
