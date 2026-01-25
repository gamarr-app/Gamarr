import { Component } from 'react';
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

interface FilterModalState {
  filterBuilder: boolean;
  id: number | null;
}

class FilterModal extends Component<FilterModalProps, FilterModalState> {
  //
  // Lifecycle

  constructor(props: FilterModalProps) {
    super(props);

    this.state = {
      filterBuilder: !props.customFilters.length,
      id: null,
    };
  }

  //
  // Listeners

  onAddCustomFilter = () => {
    this.setState({
      filterBuilder: true,
    });
  };

  onEditCustomFilter = (id: number) => {
    this.setState({
      filterBuilder: true,
      id,
    });
  };

  onCancelPress = () => {
    if (this.state.filterBuilder) {
      this.setState({
        filterBuilder: false,
        id: null,
      });
    } else {
      this.onModalClose();
    }
  };

  onModalClose = () => {
    this.setState(
      {
        filterBuilder: false,
        id: null,
      },
      () => {
        this.props.onModalClose();
      }
    );
  };

  //
  // Render

  render() {
    const { isOpen, ...otherProps } = this.props;

    const { filterBuilder, id } = this.state;

    return (
      <Modal isOpen={isOpen} onModalClose={this.onModalClose}>
        {filterBuilder ? (
          <FilterBuilderModalContentConnector
            {...otherProps}
            id={id ?? undefined}
            onCancelPress={this.onCancelPress}
            onModalClose={this.onModalClose}
          />
        ) : (
          <CustomFiltersModalContentConnector
            {...otherProps}
            onAddCustomFilter={this.onAddCustomFilter}
            onEditCustomFilter={this.onEditCustomFilter}
            onModalClose={this.onModalClose}
          />
        )}
      </Modal>
    );
  }
}

export default FilterModal;
