import classNames from 'classnames';
import { HTMLAttributes, ReactNode } from 'react';
import styles from './VirtualTableRow.css';

interface VirtualTableRowProps extends HTMLAttributes<HTMLDivElement> {
  className?: string;
  style: object;
  children?: ReactNode;
}

function VirtualTableRow({
  className,
  children,
  style,
  ...otherProps
}: VirtualTableRowProps) {
  return (
    <div
      className={classNames(styles.row, className)}
      style={style}
      {...otherProps}
    >
      {children}
    </div>
  );
}

export default VirtualTableRow;
