import React from 'react';
import { connect, ConnectedProps } from 'react-redux';
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
  filterBuilderProps: FilterBuilderProp<unknown>[];
  dispatchSetFilter: (payload: { selectedFilterKey: number | string }) => void;
  onCancelPress: () => void;
  onModalClose: () => void;
}

function createMapStateToProps() {
  return createSelector(
    (_state: AppState, { customFilters }: OwnProps) => customFilters,
    (_state: AppState, { id }: OwnProps) => id,
    (state: AppState) => state.customFilters.isSaving,
    (state: AppState) => state.customFilters.saveError,
    (customFilters, id, isSaving, saveError) => {
      if (id) {
        const customFilter = customFilters.find((c) => c.id === id);

        return {
          id: customFilter?.id,
          label: customFilter?.label ?? '',
          filters: (customFilter?.filters ?? []) as PropertyFilter[],
          customFilters,
          isSaving,
          saveError,
        };
      }

      return {
        label: '',
        filters: [] as PropertyFilter[],
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

const connector = connect(createMapStateToProps, mapDispatchToProps);

type PropsFromRedux = ConnectedProps<typeof connector>;

function FilterBuilderModalContentConnectorWrapper(
  props: OwnProps & PropsFromRedux
) {
  return <FilterBuilderModalContent {...props} />;
}

export default connector(
  FilterBuilderModalContentConnectorWrapper
) as React.ComponentType<OwnProps>;
