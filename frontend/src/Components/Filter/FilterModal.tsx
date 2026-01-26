import { useCallback, useState } from 'react';
import { CustomFilter, Filter, FilterBuilderProp } from 'App/State/AppState';
import Modal from 'Components/Modal/Modal';
import FilterBuilderModalContentConnector from './Builder/FilterBuilderModalContentConnector';
import CustomFiltersModalContentConnector from './CustomFilters/CustomFiltersModalContentConnector';

interface FilterModalProps {
  isOpen: boolean;
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  sectionItems: unknown[];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  filterBuilderProps: FilterBuilderProp<any>[];
  customFilterType: string;
  dispatchSetFilter: (payload: { selectedFilterKey: number | string }) => void;
  onFilterSelect?: (filter: string | number) => void;
  onModalClose: () => void;
}

function FilterModal(props: FilterModalProps) {
  const { isOpen, customFilters, onModalClose, ...otherProps } = props;

  const [filterBuilder, setFilterBuilder] = useState(!customFilters.length);
  const [id, setId] = useState<number | null>(null);

  const onAddCustomFilter = useCallback(() => {
    setFilterBuilder(true);
  }, []);

  const onEditCustomFilter = useCallback((editId: number) => {
    setFilterBuilder(true);
    setId(editId);
  }, []);

  const onCancelPress = useCallback(() => {
    if (filterBuilder) {
      setFilterBuilder(false);
      setId(null);
    } else {
      setFilterBuilder(false);
      setId(null);
      onModalClose();
    }
  }, [filterBuilder, onModalClose]);

  const handleModalClose = useCallback(() => {
    setFilterBuilder(false);
    setId(null);
    onModalClose();
  }, [onModalClose]);

  return (
    <Modal isOpen={isOpen} onModalClose={handleModalClose}>
      {filterBuilder ? (
        <FilterBuilderModalContentConnector
          {...otherProps}
          customFilters={customFilters}
          id={id ?? undefined}
          onCancelPress={onCancelPress}
          onModalClose={handleModalClose}
        />
      ) : (
        <CustomFiltersModalContentConnector
          {...otherProps}
          customFilters={customFilters}
          onAddCustomFilter={onAddCustomFilter}
          onEditCustomFilter={onEditCustomFilter}
          onModalClose={handleModalClose}
        />
      )}
    </Modal>
  );
}

export default FilterModal;
