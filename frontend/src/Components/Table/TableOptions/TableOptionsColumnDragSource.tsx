import classNames from 'classnames';
import { useRef } from 'react';
import { DropTargetMonitor, useDrag, useDrop } from 'react-dnd';
import { TABLE_COLUMN } from 'Helpers/dragTypes';
import { CheckInputChanged } from 'typings/inputs';
import TableOptionsColumn from './TableOptionsColumn';
import styles from './TableOptionsColumnDragSource.css';

interface DragItem {
  id?: number;
  index: number;
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
  onVisibleChange: (change: CheckInputChanged) => void;
  onColumnDragMove: (dragIndex: number, hoverIndex: number) => void;
  onColumnDragEnd: (item: DragItem, didDrop: boolean) => void;
}

function TableOptionsColumnDragSource(
  props: TableOptionsColumnDragSourceProps
) {
  const {
    name,
    label,
    isVisible,
    isModifiable,
    index,
    isDraggingUp,
    isDraggingDown,
    onVisibleChange,
    onColumnDragMove,
    onColumnDragEnd,
  } = props;

  const ref = useRef<HTMLDivElement>(null);

  const [{ isDragging }, drag, dragPreview] = useDrag({
    type: TABLE_COLUMN,
    item: () => ({ index }),
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
    end: (item, monitor) => {
      onColumnDragEnd(item, monitor.didDrop());
    },
  });

  const [{ isOver }, drop] = useDrop({
    accept: TABLE_COLUMN,
    hover: (item: DragItem, monitor: DropTargetMonitor<DragItem>) => {
      if (!ref.current) {
        return;
      }

      const dragIndex = item.index;
      const hoverIndex = index;

      const hoverBoundingRect = ref.current.getBoundingClientRect();
      const hoverMiddleY =
        (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
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

      onColumnDragMove(dragIndex, hoverIndex);
    },
    collect: (monitor) => ({
      isOver: monitor.isOver(),
    }),
  });

  dragPreview(drop(ref));

  const isBefore = !isDragging && isDraggingUp && isOver;
  const isAfter = !isDragging && isDraggingDown && isOver;

  return (
    <div ref={ref} className={styles.columnDragSource}>
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
        connectDragSource={drag}
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

export default TableOptionsColumnDragSource;
