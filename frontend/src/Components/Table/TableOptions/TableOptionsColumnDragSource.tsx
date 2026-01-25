import classNames from 'classnames';
import { Component } from 'react';
import {
  ConnectDragSource,
  ConnectDropTarget,
  DragSource,
  DragSourceConnector,
  DragSourceMonitor,
  DropTarget,
  DropTargetConnector,
  DropTargetMonitor,
} from 'react-dnd';
import { findDOMNode } from 'react-dom';
import { TABLE_COLUMN } from 'Helpers/dragTypes';
import { CheckInputChanged } from 'typings/inputs';
import TableOptionsColumn from './TableOptionsColumn';
import styles from './TableOptionsColumnDragSource.css';

interface DragItem {
  id?: number;
  index: number;
}

interface ColumnDragSourceProps {
  name: string;
  label: string | (() => string);
  isVisible: boolean;
  isModifiable: boolean;
  index: number;
  onColumnDragEnd: (item: DragItem, didDrop: boolean) => void;
}

const columnDragSource = {
  beginDrag(column: ColumnDragSourceProps): DragItem {
    return {
      index: column.index,
    };
  },

  endDrag(props: ColumnDragSourceProps, monitor: DragSourceMonitor) {
    props.onColumnDragEnd(monitor.getItem() as DragItem, monitor.didDrop());
  },
};

interface ColumnDropTargetProps {
  index: number;
  onColumnDragMove: (dragIndex: number, hoverIndex: number) => void;
}

const columnDropTarget = {
  hover(
    props: ColumnDropTargetProps,
    monitor: DropTargetMonitor<DragItem>,
    component: Component<TableOptionsColumnDragSourceProps>
  ) {
    const dragIndex = monitor.getItem().index;
    const hoverIndex = props.index;

    const componentDOMNode = findDOMNode(component) as HTMLElement;
    const hoverBoundingRect = componentDOMNode.getBoundingClientRect();
    const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
    const clientOffset = monitor.getClientOffset();
    const hoverClientY = clientOffset!.y - hoverBoundingRect.top;

    if (dragIndex === hoverIndex) {
      return;
    }

    // When moving up, only trigger if drag position is above 50% and
    // when moving down, only trigger if drag position is below 50%.
    // If we're moving down the hoverIndex needs to be increased
    // by one so it's ordered properly. Otherwise the hoverIndex will work.

    // Dragging downwards
    if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) {
      return;
    }

    // Dragging upwards
    if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) {
      return;
    }

    props.onColumnDragMove(dragIndex, hoverIndex);
  },
};

function collectDragSource(
  connect: DragSourceConnector,
  monitor: DragSourceMonitor
) {
  return {
    connectDragSource: connect.dragSource(),
    isDragging: monitor.isDragging(),
  };
}

function collectDropTarget(
  connect: DropTargetConnector,
  monitor: DropTargetMonitor
) {
  return {
    connectDropTarget: connect.dropTarget(),
    isOver: monitor.isOver(),
  };
}

interface TableOptionsColumnDragSourceProps {
  name: string;
  label: string | (() => string);
  isVisible: boolean;
  isModifiable: boolean;
  index: number;
  isDragging?: boolean;
  isDraggingUp?: boolean;
  isDraggingDown?: boolean;
  isOver?: boolean;
  connectDragSource?: ConnectDragSource;
  connectDropTarget?: ConnectDropTarget;
  onVisibleChange: (change: CheckInputChanged) => void;
  onColumnDragMove: (dragIndex: number, hoverIndex: number) => void;
  onColumnDragEnd: (item: DragItem, didDrop: boolean) => void;
}

class TableOptionsColumnDragSourceComponent extends Component<TableOptionsColumnDragSourceProps> {
  //
  // Render

  render() {
    const {
      name,
      label,
      isVisible,
      isModifiable,
      index,
      isDragging,
      isDraggingUp,
      isDraggingDown,
      isOver,
      connectDragSource,
      connectDropTarget,
      onVisibleChange,
    } = this.props;

    const isBefore = !isDragging && isDraggingUp && isOver;
    const isAfter = !isDragging && isDraggingDown && isOver;

    // if (isDragging && !isOver) {
    //   return null;
    // }

    return connectDropTarget!(
      <div className={styles.columnDragSource}>
        {isBefore && (
          <div
            className={classNames(
              styles.columnPlaceholder,
              styles.columnPlaceholderBefore
            )}
          />
        )}

        <TableOptionsColumn
          name={name}
          label={typeof label === 'function' ? label() : label}
          isVisible={isVisible}
          isModifiable={isModifiable}
          index={index}
          isDragging={isDragging}
          connectDragSource={connectDragSource}
          onVisibleChange={onVisibleChange}
        />

        {isAfter && (
          <div
            className={classNames(
              styles.columnPlaceholder,
              styles.columnPlaceholderAfter
            )}
          />
        )}
      </div>
    );
  }
}

export default DropTarget(
  TABLE_COLUMN,
  columnDropTarget,
  collectDropTarget
)(
  DragSource(
    TABLE_COLUMN,
    columnDragSource,
    collectDragSource
  )(TableOptionsColumnDragSourceComponent as unknown as React.ComponentType)
) as unknown as React.ComponentType<TableOptionsColumnDragSourceProps>;
