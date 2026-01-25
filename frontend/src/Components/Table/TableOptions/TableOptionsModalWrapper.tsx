import React, { Component, ReactElement } from 'react';
import Column from 'Components/Table/Column';
import TableOptionsModal from './TableOptionsModal';

interface TableOptionsModalWrapperProps {
  columns: Column[];
  children: ReactElement<{ onPress?: () => void }>;
  pageSize?: number;
  maxPageSize?: number;
  canModifyColumns?: boolean;
  optionsComponent?: React.ElementType;
  onTableOptionChange: (options: {
    pageSize?: number;
    columns?: Column[];
  }) => void;
}

interface TableOptionsModalWrapperState {
  isTableOptionsModalOpen: boolean;
}

class TableOptionsModalWrapper extends Component<
  TableOptionsModalWrapperProps,
  TableOptionsModalWrapperState
> {
  //
  // Lifecycle

  constructor(props: TableOptionsModalWrapperProps) {
    super(props);

    this.state = {
      isTableOptionsModalOpen: false,
    };
  }

  //
  // Listeners

  onTableOptionsPress = () => {
    this.setState({ isTableOptionsModalOpen: true });
  };

  onTableOptionsModalClose = () => {
    this.setState({ isTableOptionsModalOpen: false });
  };

  //
  // Render

  render() {
    const { columns, children, ...otherProps } = this.props;

    return (
      <>
        {React.cloneElement(children, { onPress: this.onTableOptionsPress })}

        <TableOptionsModal
          {...otherProps}
          isOpen={this.state.isTableOptionsModalOpen}
          columns={columns}
          onModalClose={this.onTableOptionsModalClose}
        />
      </>
    );
  }
}

export default TableOptionsModalWrapper;
