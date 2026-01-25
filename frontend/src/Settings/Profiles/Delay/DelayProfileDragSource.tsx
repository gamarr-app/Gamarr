import classNames from 'classnames';
import { ComponentProps, useRef } from 'react';
import { DropTargetMonitor, useDrag, useDrop } from 'react-dnd';
import { Tag } from 'App/State/TagsAppState';
import { DELAY_PROFILE } from 'Helpers/dragTypes';
import DelayProfile from './DelayProfile';
import styles from './DelayProfileDragSource.css';

interface DragItem {
  id: number;
  order: number;
}

interface DelayProfileDragSourceProps {
  id: number;
  index?: number;
  order: number;
  isDragging?: boolean;
  isDraggingUp?: boolean;
  isDraggingDown?: boolean;
  enableUsenet?: boolean;
  enableTorrent?: boolean;
  preferredProtocol?: string;
  usenetDelay?: number;
  torrentDelay?: number;
  tags?: number[];
  tagList?: Tag[];
  onConfirmDeleteDelayProfile?: (id: number) => void;
  onDelayProfileDragMove: (dragIndex: number, hoverIndex: number) => void;
  onDelayProfileDragEnd: (item: DragItem, didDrop: boolean) => void;
}

function DelayProfileDragSource(props: DelayProfileDragSourceProps) {
  const {
    id,
    order,
    isDraggingUp,
    isDraggingDown,
    onDelayProfileDragMove,
    onDelayProfileDragEnd,
    ...otherProps
  } = props;

  const ref = useRef<HTMLDivElement>(null);

  const [{ isDragging }, drag, dragPreview] = useDrag({
    type: DELAY_PROFILE,
    item: () => ({ id, order }),
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
    end: (item, monitor) => {
      onDelayProfileDragEnd(item, monitor.didDrop());
    },
  });

  const [{ isOver }, drop] = useDrop({
    accept: DELAY_PROFILE,
    hover: (item: DragItem, monitor: DropTargetMonitor<DragItem>) => {
      if (!ref.current) {
        return;
      }

      const dragIndex = item.order;
      const hoverIndex = order;

      const hoverBoundingRect = ref.current.getBoundingClientRect();
      const hoverMiddleY =
        (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
      const clientOffset = monitor.getClientOffset();
      const hoverClientY = clientOffset!.y - hoverBoundingRect.top;

      if (dragIndex === hoverIndex) {
        return;
      }

      if (dragIndex < hoverIndex && hoverClientY > hoverMiddleY) {
        onDelayProfileDragMove(dragIndex, hoverIndex + 1);
      } else if (dragIndex > hoverIndex && hoverClientY < hoverMiddleY) {
        onDelayProfileDragMove(dragIndex, hoverIndex);
      }
    },
    collect: (monitor) => ({
      isOver: monitor.isOver(),
    }),
  });

  dragPreview(drop(ref));

  const isBefore = !isDragging && isDraggingUp && isOver;
  const isAfter = !isDragging && isDraggingDown && isOver;

  return (
    <div
      ref={ref}
      className={classNames(
        styles.delayProfileDragSource,
        isBefore && styles.isDraggingUp,
        isAfter && styles.isDraggingDown
      )}
    >
      {isBefore && (
        <div
          className={classNames(
            styles.delayProfilePlaceholder,
            styles.delayProfilePlaceholderBefore
          )}
        />
      )}

      <DelayProfile
        id={id}
        isDragging={isDragging}
        dragRef={drag}
        {...(otherProps as Omit<
          ComponentProps<typeof DelayProfile>,
          'id' | 'isDragging' | 'dragRef'
        >)}
      />

      {isAfter && (
        <div
          className={classNames(
            styles.delayProfilePlaceholder,
            styles.delayProfilePlaceholderAfter
          )}
        />
      )}
    </div>
  );
}

export default DelayProfileDragSource;
