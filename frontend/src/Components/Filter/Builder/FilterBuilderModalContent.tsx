import { maxBy } from 'lodash';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
import {
  CustomFilter,
  FilterBuilderProp,
  PropertyFilter,
} from 'App/State/AppState';
import FormInputGroup from 'Components/Form/FormInputGroup';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import FilterBuilderRow from './FilterBuilderRow';
import styles from './FilterBuilderModalContent.css';

interface FilterItem {
  key?: string;
  value?: unknown;
  type?: string;
}

interface SaveCustomFilterPayload {
  id?: number;
  type: string;
  label: string;
  filters: FilterItem[];
}

interface FilterBuilderModalContentProps {
  id?: number;
  label: string;
  customFilterType: string;
  sectionItems: unknown[];
  filters: PropertyFilter[];
  filterBuilderProps: FilterBuilderProp<unknown>[];
  customFilters: CustomFilter[];
  isSaving: boolean;
  saveError?: Error | null;
  dispatchDeleteCustomFilter: (payload: { id: number }) => void;
  onSaveCustomFilterPress: (payload: SaveCustomFilterPayload) => void;
  dispatchSetFilter: (payload: { selectedFilterKey: number | string }) => void;
  onCancelPress: () => void;
  onModalClose: () => void;
}

function FilterBuilderModalContent(props: FilterBuilderModalContentProps) {
  const {
    id,
    label: labelProp,
    customFilterType,
    sectionItems,
    filters: filtersProp,
    filterBuilderProps,
    customFilters,
    isSaving,
    saveError,
    onSaveCustomFilterPress,
    dispatchSetFilter,
    onCancelPress,
    onModalClose,
  } = props;

  // Initialize filters with an empty filter if none exist
  const initialFilters = filtersProp.length ? [...filtersProp] : [{}];

  const [label, setLabel] = useState(labelProp);
  const [filters, setFilters] = useState<FilterItem[]>(initialFilters);
  const [labelErrors, setLabelErrors] = useState<Array<{ message: string }>>(
    []
  );

  const prevIsSavingRef = useRef(isSaving);

  // Handle successful save
  useEffect(() => {
    if (prevIsSavingRef.current && !isSaving && !saveError) {
      if (id) {
        dispatchSetFilter({ selectedFilterKey: id });
      } else {
        const last = maxBy(customFilters, 'id');
        if (last) {
          dispatchSetFilter({ selectedFilterKey: last.id });
        }
      }

      onModalClose();
    }
    prevIsSavingRef.current = isSaving;
  }, [isSaving, saveError, id, customFilters, dispatchSetFilter, onModalClose]);

  const onLabelChange = useCallback(({ value }: { value: string }) => {
    setLabel(value);
  }, []);

  const onFilterChange = useCallback((index: number, filter: FilterItem) => {
    setFilters((prevFilters) => {
      const newFilters = [...prevFilters];
      newFilters.splice(index, 1, filter);
      return newFilters;
    });
  }, []);

  const onAddFilterPress = useCallback(() => {
    setFilters((prevFilters) => [...prevFilters, {}]);
  }, []);

  const onRemoveFilterPress = useCallback((index: number) => {
    setFilters((prevFilters) => {
      const newFilters = [...prevFilters];
      newFilters.splice(index, 1);
      return newFilters;
    });
  }, []);

  const onSaveFilterPress = useCallback(() => {
    if (!label) {
      setLabelErrors([
        {
          message: translate('LabelIsRequired'),
        },
      ]);

      return;
    }

    onSaveCustomFilterPress({
      id,
      type: customFilterType,
      label,
      filters,
    });
  }, [id, customFilterType, label, filters, onSaveCustomFilterPress]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('CustomFilter')}</ModalHeader>

      <ModalBody>
        <div className={styles.labelContainer}>
          <div className={styles.label}>{translate('Label')}</div>

          <div className={styles.labelInputContainer}>
            <FormInputGroup
              name="label"
              value={label}
              type={inputTypes.TEXT}
              errors={labelErrors}
              onChange={onLabelChange}
            />
          </div>
        </div>

        <div className={styles.label}>{translate('Filters')}</div>

        <div className={styles.rows}>
          {filters.map((filter, index) => {
            return (
              <FilterBuilderRow
                key={`${filter.key}-${index}`}
                index={index}
                sectionItems={sectionItems}
                filterBuilderProps={filterBuilderProps}
                filterKey={filter.key}
                filterValue={
                  filter.value as
                    | string
                    | number
                    | unknown[]
                    | Record<string, unknown>
                    | undefined
                }
                filterType={filter.type}
                filterCount={filters.length}
                onAddPress={onAddFilterPress}
                onRemovePress={onRemoveFilterPress}
                onFilterChange={onFilterChange}
              />
            );
          })}
        </div>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onCancelPress}>{translate('Cancel')}</Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError ?? undefined}
          onPress={onSaveFilterPress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default FilterBuilderModalContent;
