import React from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState, { CustomFilter as CustomFilterType } from 'App/State/AppState';
import { deleteCustomFilter } from 'Store/Actions/customFilterActions';
import CustomFiltersModalContent from './CustomFiltersModalContent';

interface OwnProps {
  selectedFilterKey: string | number;
  customFilters: CustomFilterType[];
  dispatchSetFilter: (payload: { selectedFilterKey: string }) => void;
  onAddCustomFilter: () => void;
  onEditCustomFilter: (id: number) => void;
  onModalClose: () => void;
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.customFilters.isDeleting,
    (state: AppState) => state.customFilters.deleteError,
    (isDeleting, deleteError) => {
      return {
        isDeleting,
        deleteError,
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchDeleteCustomFilter: deleteCustomFilter,
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(CustomFiltersModalContent as any) as React.ComponentType<OwnProps>;
