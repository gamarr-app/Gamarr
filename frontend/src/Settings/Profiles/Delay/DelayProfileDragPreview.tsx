import { CSSProperties } from 'react';
import { useDragLayer, XYCoord } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { DELAY_PROFILE } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions';
import DelayProfile from './DelayProfile';
import styles from './DelayProfileDragPreview.css';

const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

function noop() {
  // no-op for drag preview
}

interface DragItem {
  id: number;
  enableUsenet: boolean;
  enableTorrent: boolean;
  preferredProtocol: string;
  usenetDelay: number;
  torrentDelay: number;
  tags: number[];
}

interface DelayProfileDragPreviewProps {
  width: number;
}

function DelayProfileDragPreview({ width }: DelayProfileDragPreviewProps) {
  const { item, itemType, currentOffset } = useDragLayer((monitor) => ({
    item: monitor.getItem() as DragItem | null,
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset() as XYCoord | null,
  }));

  if (!currentOffset || itemType !== DELAY_PROFILE || !item) {
    return null;
  }

  const { x, y } = currentOffset;
  const handleOffset = width - dragHandleWidth;
  const transform = `translate3d(${x - handleOffset}px, ${y}px, 0)`;

  const style: CSSProperties = {
    width,
    position: 'absolute',
    WebkitTransform: transform,
    msTransform: transform,
    transform,
  };

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

export default DelayProfileDragPreview;
