import React from 'react';
import { connect, ConnectedProps } from 'react-redux';
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

const connector = connect(createMapStateToProps, mapDispatchToProps);

type PropsFromRedux = ConnectedProps<typeof connector>;

function CustomFiltersModalContentConnectorWrapper(
  props: OwnProps & PropsFromRedux
) {
  return <CustomFiltersModalContent {...props} />;
}

export default connector(
  CustomFiltersModalContentConnectorWrapper
) as React.ComponentType<OwnProps>;
