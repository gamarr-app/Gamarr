/* eslint-disable @typescript-eslint/no-explicit-any */
import React, { Component } from 'react';
import { DragLayer } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { DELAY_PROFILE } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions.js';
import DelayProfile from './DelayProfile';
import styles from './DelayProfileDragPreview.css';

const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

function collectDragLayer(monitor: any) {
  return {
    item: monitor.getItem(),
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset(),
  };
}

interface DelayProfileDragPreviewProps {
  width: number;
  item?: any;
  itemType?: string;
  currentOffset?: { x: number; y: number };
}

class DelayProfileDragPreviewComponent extends Component<DelayProfileDragPreviewProps> {
  //
  // Render

  render() {
    const { width, item, itemType, currentOffset } = this.props;

    if (!currentOffset || itemType !== DELAY_PROFILE) {
      return null;
    }

    const { x, y } = currentOffset;
    const handleOffset = width - dragHandleWidth;
    const transform = `translate3d(${x - handleOffset}px, ${y}px, 0)`;

    const style: React.CSSProperties = {
      width,
      position: 'absolute',
      WebkitTransform: transform,
      msTransform: transform,
      transform,
    };

    return (
      <DragPreviewLayer>
        <div className={styles.dragPreview} style={style}>
          <DelayProfile isDragging={false} {...item} />
        </div>
      </DragPreviewLayer>
    );
  }
}

export default DragLayer(collectDragLayer)(
  DelayProfileDragPreviewComponent as any
) as any;
