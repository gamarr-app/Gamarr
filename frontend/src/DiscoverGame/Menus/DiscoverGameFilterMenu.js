import PropTypes from 'prop-types';
import React from 'react';
import FilterMenu from 'Components/Menu/FilterMenu';
import DiscoverGameFilterModalConnector from 'DiscoverGame/DiscoverGameFilterModalConnector';
import { align } from 'Helpers/Props';

function DiscoverGameFilterMenu(props) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    isDisabled,
    onFilterSelect
  } = props;

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

DiscoverGameFilterMenu.propTypes = {
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool.isRequired,
  onFilterSelect: PropTypes.func.isRequired
};

DiscoverGameFilterMenu.defaultProps = {
  showCustomFilters: false
};

export default DiscoverGameFilterMenu;
