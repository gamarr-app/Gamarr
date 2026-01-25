import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setReleasesFilter } from 'Store/Actions/releaseActions';

function createReleasesSelector() {
  return createSelector(
    (state: AppState) => state.releases.items,
    (releases) => {
      return releases;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state: AppState) => state.releases.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

interface InteractiveSearchFilterModalProps {
  isOpen: boolean;
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  onFilterSelect: (filter: string | number) => void;
  onModalClose: () => void;
}

export default function InteractiveSearchFilterModal({
  isOpen,
  selectedFilterKey,
  filters,
  customFilters,
  onFilterSelect,
  onModalClose,
}: InteractiveSearchFilterModalProps) {
  const sectionItems = useSelector(createReleasesSelector());
  const filterBuilderProps = useSelector(createFilterBuilderPropsSelector());

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: unknown) => {
      dispatch(setReleasesFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      isOpen={isOpen}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      sectionItems={sectionItems}
      filterBuilderProps={filterBuilderProps}
      customFilterType="releases"
      dispatchSetFilter={dispatchSetFilter}
      onFilterSelect={onFilterSelect}
      onModalClose={onModalClose}
    />
  );
}
