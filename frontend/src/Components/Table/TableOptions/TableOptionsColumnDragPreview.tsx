import { useDragLayer, XYCoord } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { TABLE_COLUMN } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions.js';
import TableOptionsColumn from './TableOptionsColumn';
import styles from './TableOptionsColumnDragPreview.css';

const formGroupSmallWidth = parseInt(dimensions.formGroupSmallWidth);
const formLabelLargeWidth = parseInt(dimensions.formLabelLargeWidth);
const formLabelRightMarginWidth = parseInt(dimensions.formLabelRightMarginWidth);
const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

interface DragItem {
  name: string;
  label: string;
  isVisible: boolean;
  index: number;
}

function TableOptionsColumnDragPreview() {
  const { item, itemType, currentOffset } = useDragLayer((monitor) => ({
    item: monitor.getItem() as DragItem | null,
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset() as XYCoord | null,
  }));

  if (!currentOffset || itemType !== TABLE_COLUMN || !item) {
    return null;
  }

  // The offset is shifted because the drag handle is on the right edge of the
  // list item and the preview is wider than the drag handle.
  const { x, y } = currentOffset;
  const handleOffset = formGroupSmallWidth - formLabelLargeWidth - formLabelRightMarginWidth - dragHandleWidth;
  const transform = `translate3d(${x - handleOffset}px, ${y}px, 0)`;

  const style: React.CSSProperties = {
    position: 'absolute',
    WebkitTransform: transform,
    msTransform: transform,
    transform,
  };

  return (
    <DragPreviewLayer>
      <div className={styles.dragPreview} style={style}>
        <TableOptionsColumn
          isDragging={false}
          isModifiable={false}
          onVisibleChange={() => {}}
          {...item}
        />
      </div>
    </DragPreviewLayer>
  );
}

export default TableOptionsColumnDragPreview;
