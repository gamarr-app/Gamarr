import { maxBy } from 'lodash';
import React, { Component } from 'react';
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

interface FilterBuilderModalContentState {
  label: string;
  filters: FilterItem[];
  labelErrors: Array<{ message: string }>;
}

class FilterBuilderModalContent extends Component<
  FilterBuilderModalContentProps,
  FilterBuilderModalContentState
> {
  //
  // Lifecycle

  constructor(props: FilterBuilderModalContentProps) {
    super(props);

    const filters: FilterItem[] = [...props.filters];

    // Push an empty filter if there aren't any filters. FilterBuilderRow
    // will handle initializing the filter.

    if (!filters.length) {
      filters.push({});
    }

    this.state = {
      label: props.label,
      filters,
      labelErrors: [],
    };
  }

  componentDidUpdate(prevProps: FilterBuilderModalContentProps) {
    const {
      id,
      customFilters,
      isSaving,
      saveError,
      dispatchSetFilter,
      onModalClose,
    } = this.props;

    if (prevProps.isSaving && !isSaving && !saveError) {
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
  }

  //
  // Listeners

  onLabelChange = ({ value }: { value: string }) => {
    this.setState({ label: value });
  };

  onFilterChange = (index: number, filter: FilterItem) => {
    const filters = [...this.state.filters];
    filters.splice(index, 1, filter);

    this.setState({
      filters,
    });
  };

  onAddFilterPress = () => {
    const filters = [...this.state.filters];
    filters.push({});

    this.setState({
      filters,
    });
  };

  onRemoveFilterPress = (index: number) => {
    const filters = [...this.state.filters];
    filters.splice(index, 1);

    this.setState({
      filters,
    });
  };

  onSaveFilterPress = () => {
    const { id, customFilterType, onSaveCustomFilterPress } = this.props;

    const { label, filters } = this.state;

    if (!label) {
      this.setState({
        labelErrors: [
          {
            message: translate('LabelIsRequired'),
          },
        ],
      });

      return;
    }

    onSaveCustomFilterPress({
      id,
      type: customFilterType,
      label,
      filters,
    });
  };

  //
  // Render

  render() {
    const {
      sectionItems,
      filterBuilderProps,
      isSaving,
      saveError,
      onCancelPress,
      onModalClose,
    } = this.props;

    const { label, filters, labelErrors } = this.state;

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
                onChange={this.onLabelChange}
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
                  onAddPress={this.onAddFilterPress}
                  onRemovePress={this.onRemoveFilterPress}
                  onFilterChange={this.onFilterChange}
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
            onPress={this.onSaveFilterPress}
          >
            {translate('Save')}
          </SpinnerErrorButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

export default FilterBuilderModalContent;
