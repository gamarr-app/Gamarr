import classNames from 'classnames';
import React, { Component } from 'react';
import { DragSource, DropTarget } from 'react-dnd';
import { findDOMNode } from 'react-dom';
import { DELAY_PROFILE } from 'Helpers/dragTypes';
import DelayProfile from './DelayProfile';
import styles from './DelayProfileDragSource.css';

const delayProfileDragSource = {
  beginDrag(item: any) {
    return item;
  },

  endDrag(props: any, monitor: any) {
    props.onDelayProfileDragEnd(monitor.getItem(), monitor.didDrop());
  },
};

const delayProfileDropTarget = {
  hover(props: any, monitor: any, component: any) {
    const dragIndex = monitor.getItem().order;
    const hoverIndex = props.order;

    // eslint-disable-next-line react/no-find-dom-node
    const hoverBoundingRect = (
      findDOMNode(component) as HTMLElement
    ).getBoundingClientRect();
    const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
    const clientOffset = monitor.getClientOffset();
    const hoverClientY = clientOffset.y - hoverBoundingRect.top;

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

function collectDragSource(connect: any, monitor: any) {
  return {
    connectDragSource: connect.dragSource(),
    isDragging: monitor.isDragging(),
  };
}

function collectDropTarget(connect: any, monitor: any) {
  return {
    connectDropTarget: connect.dropTarget(),
    isOver: monitor.isOver(),
  };
}

interface DelayProfileDragSourceProps {
  id: number;
  order: number;
  isDragging?: boolean;
  isDraggingUp?: boolean;
  isDraggingDown?: boolean;
  isOver?: boolean;
  connectDragSource?: any;
  connectDropTarget?: any;
  onDelayProfileDragMove: (dragIndex: number, hoverIndex: number) => void;
  onDelayProfileDragEnd: (item: any, didDrop: boolean) => void;
  [key: string]: any;
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

    return connectDropTarget(
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
          {...(otherProps as any)}
          connectDragSource={connectDragSource}
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
  )(DelayProfileDragSourceComponent as any)
) as any;
