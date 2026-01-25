import { ComponentType, ReactNode } from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import { GameStatus } from 'Game/Game';
import getGameStatusDetails from 'Game/getGameStatusDetails';
import styles from './ListGameStatusCell.css';

interface ListGameStatusCellProps {
  className: string;
  status: string;
  isExclusion?: boolean;
  isExisting?: boolean;
  component?: ComponentType<{ className?: string; children?: ReactNode }>;
}

function ListGameStatusCell({
  className,
  status,
  component: Component = VirtualTableRowCell,
  ...otherProps
}: ListGameStatusCellProps) {
  const statusDetails = getGameStatusDetails(status as GameStatus);

  return (
    <Component className={className} {...otherProps}>
      <Icon
        className={styles.statusIcon}
        name={statusDetails.icon}
        title={`${statusDetails.title}: ${statusDetails.message}`}
      />
    </Component>
  );
}

export default ListGameStatusCell;
