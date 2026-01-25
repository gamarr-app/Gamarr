import { HTMLAttributes, ReactNode } from 'react';
import styles from './TableRow.css';

interface TableRowProps extends HTMLAttributes<HTMLTableRowElement> {
  className?: string;
  children?: ReactNode;
  overlayContent?: boolean;
}

function TableRow({
  className = styles.row,
  children,
  overlayContent,
  ...otherProps
}: TableRowProps) {
  return (
    <tr className={className} {...otherProps}>
      {children}
    </tr>
  );
}

export default TableRow;
