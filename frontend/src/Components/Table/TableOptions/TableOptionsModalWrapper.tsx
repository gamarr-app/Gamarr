import React, { ReactElement, useCallback, useState } from 'react';
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

function TableOptionsModalWrapper(props: TableOptionsModalWrapperProps) {
  const { columns, children, ...otherProps } = props;

  const [isTableOptionsModalOpen, setIsTableOptionsModalOpen] = useState(false);

  const onTableOptionsPress = useCallback(() => {
    setIsTableOptionsModalOpen(true);
  }, []);

  const onTableOptionsModalClose = useCallback(() => {
    setIsTableOptionsModalOpen(false);
  }, []);

  return (
    <>
      {React.cloneElement(children, { onPress: onTableOptionsPress })}

      <TableOptionsModal
        {...otherProps}
        isOpen={isTableOptionsModalOpen}
        columns={columns}
        onModalClose={onTableOptionsModalClose}
      />
    </>
  );
}

export default TableOptionsModalWrapper;
