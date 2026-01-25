import React, { Component } from 'react';
import { DragLayer, DragLayerMonitor } from 'react-dnd';
import DragPreviewLayer from 'Components/DragPreviewLayer';
import { QUALITY_PROFILE_ITEM } from 'Helpers/dragTypes';
import dimensions from 'Styles/Variables/dimensions.js';
import QualityProfileItem from './QualityProfileItem';
import styles from './QualityProfileItemDragPreview.css';

const formGroupExtraSmallWidth = parseInt(dimensions.formGroupExtraSmallWidth);
const formLabelSmallWidth = parseInt(dimensions.formLabelSmallWidth);
const formLabelRightMarginWidth = parseInt(
  dimensions.formLabelRightMarginWidth
);
const dragHandleWidth = parseInt(dimensions.dragHandleWidth);

function collectDragLayer(monitor: DragLayerMonitor) {
  return {
    item: monitor.getItem(),
    itemType: monitor.getItemType(),
    currentOffset: monitor.getSourceClientOffset(),
  };
}

interface QualityProfileItemDragPreviewProps {
  item?: {
    editGroups: boolean;
    groupId?: number;
    qualityId?: number;
    name: string;
    allowed: boolean;
  };
  itemType?: string;
  currentOffset?: { x: number; y: number };
}

class QualityProfileItemDragPreviewComponent extends Component<QualityProfileItemDragPreviewProps> {
  //
  // Render

  render() {
    const { item, itemType, currentOffset } = this.props;

    if (!currentOffset || itemType !== QUALITY_PROFILE_ITEM || !item) {
      return null;
    }

    const { x, y } = currentOffset;
    const handleOffset =
      formGroupExtraSmallWidth -
      formLabelSmallWidth -
      formLabelRightMarginWidth -
      dragHandleWidth;
    const transform = `translate3d(${x - handleOffset}px, ${y}px, 0)`;

    const style: React.CSSProperties = {
      position: 'absolute',
      WebkitTransform: transform,
      msTransform: transform,
      transform,
    };

    const { editGroups, groupId, qualityId, name, allowed } = item;

    return (
      <DragPreviewLayer>
        <div className={styles.dragPreview} style={style}>
          <QualityProfileItem
            editGroups={editGroups}
            isPreview={true}
            qualityId={groupId ?? qualityId ?? 0}
            name={name}
            allowed={allowed}
            isDragging={false}
          />
        </div>
      </DragPreviewLayer>
    );
  }
}

export default DragLayer(collectDragLayer)(
  QualityProfileItemDragPreviewComponent as unknown as React.ComponentType
) as unknown as React.ComponentType;
