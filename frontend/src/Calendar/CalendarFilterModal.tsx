import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState, { CustomFilter, Filter } from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setCalendarFilter } from 'Store/Actions/calendarActions';

function createCalendarSelector() {
  return createSelector(
    (state: AppState) => state.calendar.items,
    (calendar) => {
      return calendar;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state: AppState) => state.calendar.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

interface CalendarFilterModalProps {
  isOpen: boolean;
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  onFilterSelect: (filter: string | number) => void;
  onModalClose: () => void;
}

export default function CalendarFilterModal({
  isOpen,
  selectedFilterKey,
  filters,
  customFilters,
  onFilterSelect,
  onModalClose,
}: CalendarFilterModalProps) {
  const sectionItems = useSelector(createCalendarSelector());
  const filterBuilderProps = useSelector(createFilterBuilderPropsSelector());
  const customFilterType = 'calendar';

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: unknown) => {
      dispatch(setCalendarFilter(payload));
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
