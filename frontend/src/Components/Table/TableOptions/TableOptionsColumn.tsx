import classNames from 'classnames';
import React from 'react';
import { ConnectDragSource } from 'react-dnd';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import styles from './TableOptionsColumn.css';

interface TableOptionsColumnProps {
  name: string;
  label: string;
  isVisible: boolean;
  isModifiable: boolean;
  index: number;
  isDragging?: boolean;
  isOver?: boolean;
  dragRef?: ConnectDragSource;
  onVisibleChange: (payload: { name: string; value: boolean }) => void;
}

function TableOptionsColumn(props: TableOptionsColumnProps) {
  const {
    name,
    label,
    isVisible,
    isModifiable,
    isDragging,
    dragRef,
    onVisibleChange,
  } = props;

  return (
    <div className={isModifiable ? undefined : styles.notDragable}>
      <div
        className={classNames(styles.column, isDragging && styles.isDragging)}
      >
        <label className={styles.label}>
          <CheckInput
            containerClassName={styles.checkContainer}
            name={name}
            value={isVisible}
            isDisabled={isModifiable === false}
            onChange={onVisibleChange}
          />
          {label}
        </label>

        {dragRef && (
          <div
            ref={dragRef as unknown as React.Ref<HTMLDivElement>}
            className={styles.dragHandle}
          >
            <Icon className={styles.dragIcon} name={icons.REORDER} />
          </div>
        )}
      </div>
    </div>
  );
}

export default TableOptionsColumn;
