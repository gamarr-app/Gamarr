import React, { ComponentPropsWithoutRef } from 'react';
import styles from './VirtualTableRowCell.css';

export type VirtualTableRowCellProps = ComponentPropsWithoutRef<'div'>;

function VirtualTableRowCell({
  className = styles.cell,
  children,
  ...otherProps
}: VirtualTableRowCellProps) {
  return (
    <div className={className} {...otherProps}>
      {children}
    </div>
  );
}

export default VirtualTableRowCell;
