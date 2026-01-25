import React from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState, {
  CustomFilter,
  FilterBuilderProp,
  PropertyFilter,
} from 'App/State/AppState';
import {
  deleteCustomFilter,
  saveCustomFilter,
} from 'Store/Actions/customFilterActions';
import FilterBuilderModalContent from './FilterBuilderModalContent';

interface OwnProps {
  id?: number;
  customFilters: CustomFilter[];
  customFilterType: string;
  sectionItems: unknown[];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  filterBuilderProps: FilterBuilderProp<any>[];
  dispatchSetFilter: (payload: { selectedFilterKey: number | string }) => void;
  onCancelPress: () => void;
  onModalClose: () => void;
}

interface StateProps {
  id?: number;
  label: string;
  filters: PropertyFilter[];
  customFilters: CustomFilter[];
  isSaving: boolean;
  saveError: unknown;
}

function createMapStateToProps() {
  return createSelector(
    (_state: AppState, { customFilters }: OwnProps) => customFilters,
    (_state: AppState, { id }: OwnProps) => id,
    (state: AppState) => state.customFilters.isSaving,
    (state: AppState) => state.customFilters.saveError,
    (customFilters, id, isSaving, saveError): StateProps => {
      if (id) {
        const customFilter = customFilters.find((c) => c.id === id);

        return {
          id: customFilter?.id,
          label: customFilter?.label ?? '',
          filters: customFilter?.filters ?? [],
          customFilters,
          isSaving,
          saveError,
        };
      }

      return {
        label: '',
        filters: [],
        customFilters,
        isSaving,
        saveError,
      };
    }
  );
}

const mapDispatchToProps = {
  onSaveCustomFilterPress: saveCustomFilter,
  dispatchDeleteCustomFilter: deleteCustomFilter,
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(FilterBuilderModalContent as any) as React.ComponentType<OwnProps>;
