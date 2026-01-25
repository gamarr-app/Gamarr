import classNames from 'classnames';
import React from 'react';
import { ConnectDragSource } from 'react-dnd';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import { CheckInputChanged } from 'typings/inputs';
import styles from './TableOptionsColumn.css';

interface TableOptionsColumnProps {
  name: string;
  label: string | (() => string);
  isVisible: boolean;
  isModifiable: boolean;
  index: number;
  isDragging?: boolean;
  connectDragSource?: ConnectDragSource;
  onVisibleChange: (change: CheckInputChanged) => void;
}

function TableOptionsColumn(props: TableOptionsColumnProps) {
  const {
    name,
    label,
    isVisible,
    isModifiable,
    isDragging,
    connectDragSource,
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
          {typeof label === 'function' ? label() : label}
        </label>

        {!!connectDragSource &&
          connectDragSource(
            <div className={styles.dragHandle}>
              <Icon className={styles.dragIcon} name={icons.REORDER} />
            </div>
          )}
      </div>
    </div>
  );
}

export default TableOptionsColumn;
