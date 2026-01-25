import React, { Component, CSSProperties } from 'react';
import { DragLayer, DragLayerMonitor } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { TABLE_COLUMN } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions';
import TableOptionsColumn from './TableOptionsColumn';
import styles from './TableOptionsColumnDragPreview.css';

const formGroupSmallWidth = parseInt(dimensions.formGroupSmallWidth);
const formLabelLargeWidth = parseInt(dimensions.formLabelLargeWidth);
const formLabelRightMarginWidth = parseInt(
  dimensions.formLabelRightMarginWidth
);
const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

interface DragItem {
  name: string;
  label: string | (() => string);
  isVisible: boolean;
  isModifiable: boolean;
  index: number;
}

function collectDragLayer(monitor: DragLayerMonitor) {
  return {
    item: monitor.getItem() as DragItem | null,
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset(),
  };
}

interface TableOptionsColumnDragPreviewProps {
  item?: DragItem | null;
  itemType?: string | symbol | null;
  currentOffset?: { x: number; y: number } | null;
}

class TableOptionsColumnDragPreviewComponent extends Component<TableOptionsColumnDragPreviewProps> {
  //
  // Render

  render() {
    const { item, itemType, currentOffset } = this.props;

    if (!currentOffset || itemType !== TABLE_COLUMN || !item) {
      return null;
    }

    // The offset is shifted because the drag handle is on the right edge of the
    // list item and the preview is wider than the drag handle.

    const { x, y } = currentOffset;
    const handleOffset =
      formGroupSmallWidth -
      formLabelLargeWidth -
      formLabelRightMarginWidth -
      dragHandleWidth;
    const transform = `translate3d(${x - handleOffset}px, ${y}px, 0)`;

    const style: CSSProperties = {
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
            {...item}
            onVisibleChange={() => {
              // no-op for preview
            }}
          />
        </div>
      </DragPreviewLayer>
    );
  }
}

export default DragLayer(collectDragLayer)(
  TableOptionsColumnDragPreviewComponent as unknown as React.ComponentType
) as unknown as React.ComponentType;
