import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setGameCollectionsFilter } from 'Store/Actions/gameCollectionActions';

interface GameCollectionFilterModalProps {
  isOpen: boolean;
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  onFilterSelect: (filter: string | number) => void;
  onModalClose: () => void;
}

export default function GameCollectionFilterModal({
  isOpen,
  selectedFilterKey,
  filters,
  customFilters,
  onFilterSelect,
  onModalClose,
}: GameCollectionFilterModalProps) {
  const sectionItems = useSelector(
    (state: AppState) => state.gameCollections.items
  );
  const filterBuilderProps = useSelector(
    (state: AppState) => state.gameCollections.filterBuilderProps
  );

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: { selectedFilterKey: string | number }) => {
      dispatch(setGameCollectionsFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      isOpen={isOpen}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      onFilterSelect={onFilterSelect}
      onModalClose={onModalClose}
      sectionItems={sectionItems}
      filterBuilderProps={filterBuilderProps}
      customFilterType="gameCollections"
      dispatchSetFilter={dispatchSetFilter}
    />
  );
}
