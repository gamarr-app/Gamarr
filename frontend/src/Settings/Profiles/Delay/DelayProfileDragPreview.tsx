import { Component } from 'react';
import { DragLayer, DragLayerMonitor } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { DELAY_PROFILE } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions.js';
import DelayProfile from './DelayProfile';
import styles from './DelayProfileDragPreview.css';

const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

function noop() {
  // no-op for drag preview
}

function collectDragLayer(monitor: DragLayerMonitor) {
  return {
    item: monitor.getItem(),
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset(),
  };
}

interface DelayProfileDragPreviewProps {
  width: number;
  item?: {
    id: number;
    enableUsenet: boolean;
    enableTorrent: boolean;
    preferredProtocol: string;
    usenetDelay: number;
    torrentDelay: number;
    tags: number[];
  };
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

    if (!item) {
      return null;
    }

    return (
      <DragPreviewLayer>
        <div className={styles.dragPreview} style={style}>
          <DelayProfile
            isDragging={false}
            tagList={[]}
            onConfirmDeleteDelayProfile={noop}
            {...item}
          />
        </div>
      </DragPreviewLayer>
    );
  }
}

export default DragLayer(collectDragLayer)(
  DelayProfileDragPreviewComponent as unknown as React.ComponentType
) as unknown as React.ComponentType<{ width: number }>;
