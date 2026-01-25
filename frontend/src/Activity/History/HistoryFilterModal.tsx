import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setHistoryFilter } from 'Store/Actions/historyActions';

function createHistorySelector() {
  return createSelector(
    (state: AppState) => state.history.items,
    (queueItems) => {
      return queueItems;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state: AppState) => state.history.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

interface HistoryFilterModalProps {
  isOpen: boolean;
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  onFilterSelect: (filter: string | number) => void;
  onModalClose: () => void;
}

export default function HistoryFilterModal({
  isOpen,
  selectedFilterKey,
  filters,
  customFilters,
  onFilterSelect,
  onModalClose,
}: HistoryFilterModalProps) {
  const sectionItems = useSelector(createHistorySelector());
  const filterBuilderProps = useSelector(createFilterBuilderPropsSelector());
  const customFilterType = 'history';

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: unknown) => {
      dispatch(setHistoryFilter(payload));
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
      customFilterType={customFilterType}
      dispatchSetFilter={dispatchSetFilter}
      onFilterSelect={onFilterSelect}
      onModalClose={onModalClose}
    />
  );
}
