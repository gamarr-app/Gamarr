import classNames from 'classnames';
import React, { Component } from 'react';
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
import { Tag } from 'App/State/TagsAppState';
import { DELAY_PROFILE } from 'Helpers/dragTypes';
import DelayProfile from './DelayProfile';
import styles from './DelayProfileDragSource.css';

interface DragItem {
  id: number;
  order: number;
  [key: string]: unknown;
}

interface DragSourceProps extends DragItem {
  onDelayProfileDragEnd: (item: DragItem, didDrop: boolean) => void;
}

const delayProfileDragSource = {
  beginDrag(props: DragSourceProps): DragItem {
    return {
      id: props.id,
      order: props.order,
    };
  },

  endDrag(props: DragSourceProps, monitor: DragSourceMonitor<DragItem>) {
    props.onDelayProfileDragEnd(monitor.getItem(), monitor.didDrop());
  },
};

interface DropTargetProps {
  order: number;
  onDelayProfileDragMove: (dragIndex: number, hoverIndex: number) => void;
}

const delayProfileDropTarget = {
  hover(
    props: DropTargetProps,
    monitor: DropTargetMonitor<DragItem>,
    component: Component
  ) {
    const dragIndex = monitor.getItem().order;
    const hoverIndex = props.order;

    const hoverBoundingRect = (
      findDOMNode(component) as HTMLElement
    ).getBoundingClientRect();
    const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
    const clientOffset = monitor.getClientOffset();
    const hoverClientY = clientOffset!.y - hoverBoundingRect.top;

    if (dragIndex === hoverIndex) {
      return;
    }

    if (dragIndex < hoverIndex && hoverClientY > hoverMiddleY) {
      props.onDelayProfileDragMove(dragIndex, hoverIndex + 1);
    } else if (dragIndex > hoverIndex && hoverClientY < hoverMiddleY) {
      props.onDelayProfileDragMove(dragIndex, hoverIndex);
    }
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

interface DelayProfileDragSourceProps {
  id: number;
  index?: number;
  order: number;
  isDragging?: boolean;
  isDraggingUp?: boolean;
  isDraggingDown?: boolean;
  isOver?: boolean;
  connectDragSource?: ConnectDragSource | undefined;
  connectDropTarget?: ConnectDropTarget | undefined;
  enableUsenet?: boolean;
  enableTorrent?: boolean;
  preferredProtocol?: string;
  usenetDelay?: number;
  torrentDelay?: number;
  tags?: number[];
  tagList?: Tag[];
  onConfirmDeleteDelayProfile?: (id: number) => void;
  onDelayProfileDragMove: (dragIndex: number, hoverIndex: number) => void;
  onDelayProfileDragEnd: (
    item: { id: number; [key: string]: unknown },
    didDrop: boolean
  ) => void;
}

class DelayProfileDragSourceComponent extends Component<DelayProfileDragSourceProps> {
  //
  // Render

  render() {
    const {
      id,
      order,
      isDragging,
      isDraggingUp,
      isDraggingDown,
      isOver,
      connectDragSource,
      connectDropTarget,
      ...otherProps
    } = this.props;

    const isBefore = !isDragging && isDraggingUp && isOver;
    const isAfter = !isDragging && isDraggingDown && isOver;

    return connectDropTarget!(
      <div
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
          isDragging={!!isDragging}
          {...(otherProps as unknown as Omit<
            React.ComponentProps<typeof DelayProfile>,
            'id' | 'isDragging' | 'connectDragSource'
          >)}
          connectDragSource={connectDragSource!}
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
}

export default DropTarget(
  DELAY_PROFILE,
  delayProfileDropTarget,
  collectDropTarget
)(
  DragSource(
    DELAY_PROFILE,
    delayProfileDragSource,
    collectDragSource
  )(DelayProfileDragSourceComponent as unknown as React.ComponentType)
) as unknown as React.ComponentType<DelayProfileDragSourceProps>;
