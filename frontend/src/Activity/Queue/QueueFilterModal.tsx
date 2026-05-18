import { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setQueueFilter } from 'Store/Actions/queueActions';

function createQueueSelector() {
  return createSelector(
    (state: AppState) => state.queue.paged.items,
    (queueItems) => {
      return queueItems;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state: AppState) => state.queue.paged.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

interface QueueFilterModalProps {
  isOpen: boolean;
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  onFilterSelect: (filter: string | number) => void;
  onModalClose: () => void;
}

export default function QueueFilterModal({
  isOpen,
  selectedFilterKey,
  filters,
  customFilters,
  onFilterSelect,
  onModalClose,
}: QueueFilterModalProps) {
  const sectionItems = useSelector(useMemo(() => createQueueSelector(), []));
  const filterBuilderProps = useSelector(
    useMemo(() => createFilterBuilderPropsSelector(), [])
  );
  const customFilterType = 'queue';

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: unknown) => {
      dispatch(setQueueFilter(payload));
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
