import React from 'react';
import Column from 'Components/Table/Column';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import styles from './SelectGameModalTableHeader.css';

interface SelectGameModalTableHeaderProps {
  columns: Column[];
}

function SelectGameModalTableHeader(props: SelectGameModalTableHeaderProps) {
  const { columns } = props;

  return (
    <VirtualTableHeader>
      {columns.map((column) => {
        const { name, label, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        return (
          <VirtualTableHeaderCell
            key={name}
            className={styles[name as keyof typeof styles]}
            name={name}
          >
            {typeof label === 'function' ? label() : label}
          </VirtualTableHeaderCell>
        );
      })}
    </VirtualTableHeader>
  );
}

export default SelectGameModalTableHeader;
