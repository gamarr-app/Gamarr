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
import { QUALITY_PROFILE_ITEM } from 'Helpers/dragTypes';
import QualityProfileItem from './QualityProfileItem';
import QualityProfileItemGroup from './QualityProfileItemGroup';
import styles from './QualityProfileItemDragSource.css';

interface DragSourceProps {
  editGroups: boolean;
  qualityIndex: string;
  groupId?: number;
  qualityId?: number;
  name: string;
  allowed: boolean;
  onQualityProfileItemDragEnd: (didDrop: boolean) => void;
}

const qualityProfileItemDragSource = {
  beginDrag(props: DragSourceProps) {
    const { editGroups, qualityIndex, groupId, qualityId, name, allowed } =
      props;

    return {
      editGroups,
      qualityIndex,
      groupId,
      qualityId,
      isGroup: !qualityId,
      name,
      allowed,
    };
  },

  endDrag(props: DragSourceProps) {
    props.onQualityProfileItemDragEnd(true);
  },
};

interface DropTargetProps {
  qualityIndex: string;
  qualityId?: number;
  groupId?: number;
  isOverCurrent?: boolean;
  isDraggingUp?: boolean;
  onQualityProfileItemDragMove: (payload: {
    dragQualityIndex: string;
    dropQualityIndex: string;
    dropPosition: string;
  }) => void;
}

interface DragItem {
  qualityIndex: string;
  isGroup: boolean;
}

const qualityProfileItemDropTarget = {
  hover(
    props: DropTargetProps,
    monitor: DropTargetMonitor<DragItem>,
    component: Component<DropTargetProps>
  ) {
    const { qualityIndex: dragQualityIndex, isGroup: isDragGroup } =
      monitor.getItem();

    const dropQualityIndex = props.qualityIndex;
    const isDropGroupItem = !!(props.qualityId && props.groupId);

    const childNodeIndex =
      component.props.isOverCurrent && component.props.isDraggingUp ? 1 : 0;
    const componentDOMNode = findDOMNode(component) as HTMLElement;
    const hoverBoundingRect =
      componentDOMNode.children[childNodeIndex].getBoundingClientRect();
    const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
    const clientOffset = monitor.getClientOffset();
    const hoverClientY = clientOffset!.y - hoverBoundingRect.top;

    if (!monitor.isOver({ shallow: true })) {
      return;
    }

    if (dragQualityIndex === dropQualityIndex) {
      return;
    }

    if (isDragGroup && isDropGroupItem) {
      return;
    }

    let dropPosition = null;

    if (hoverClientY > hoverMiddleY) {
      dropPosition = 'below';
    } else if (hoverClientY < hoverMiddleY) {
      dropPosition = 'above';
    } else {
      return;
    }

    props.onQualityProfileItemDragMove({
      dragQualityIndex,
      dropQualityIndex,
      dropPosition,
    });
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
    isOverCurrent: monitor.isOver({ shallow: true }),
  };
}

interface QualityProfileItemDragSourceProps {
  editGroups: boolean;
  groupId?: number;
  qualityId?: number;
  name: string;
  allowed: boolean;
  items?: Array<{ quality: { id: number; name: string } }>;
  qualityIndex: string;
  isDragging?: boolean;
  isDraggingUp?: boolean;
  isDraggingDown?: boolean;
  isOverCurrent?: boolean;
  isInGroup?: boolean;
  connectDragSource?: ConnectDragSource;
  connectDropTarget?: ConnectDropTarget;
  onCreateGroupPress?: (qualityId: number) => void;
  onDeleteGroupPress?: (groupId: number, value?: boolean) => void;
  onQualityProfileItemAllowedChange: (
    qualityId: number,
    value: boolean
  ) => void;
  onItemGroupAllowedChange?: (groupId: number, value: boolean) => void;
  onItemGroupNameChange?: (groupId: number, value: string) => void;
  onQualityProfileItemDragMove: (payload: {
    dragQualityIndex: string;
    dropQualityIndex: string;
    dropPosition: string;
  }) => void;
  onQualityProfileItemDragEnd: (didDrop: boolean) => void;
}

class QualityProfileItemDragSourceComponent extends Component<QualityProfileItemDragSourceProps> {
  //
  // Render

  render() {
    const {
      editGroups,
      groupId,
      qualityId,
      name,
      allowed,
      items,
      qualityIndex,
      isDragging,
      isDraggingUp,
      isDraggingDown,
      isOverCurrent,
      connectDragSource,
      connectDropTarget,
      onCreateGroupPress,
      onDeleteGroupPress,
      onQualityProfileItemAllowedChange,
      onItemGroupAllowedChange,
      onItemGroupNameChange,
      onQualityProfileItemDragMove,
      onQualityProfileItemDragEnd,
    } = this.props;

    const isBefore = !isDragging && isDraggingUp && isOverCurrent;
    const isAfter = !isDragging && isDraggingDown && isOverCurrent;

    return connectDropTarget!(
      <div
        className={classNames(
          styles.qualityProfileItemDragSource,
          isBefore && styles.isDraggingUp,
          isAfter && styles.isDraggingDown
        )}
      >
        {isBefore && (
          <div
            className={classNames(
              styles.qualityProfileItemPlaceholder,
              styles.qualityProfileItemPlaceholderBefore
            )}
          />
        )}

        {!!groupId && qualityId == null && (
          <QualityProfileItemGroup
            editGroups={editGroups}
            groupId={groupId}
            name={name}
            allowed={allowed}
            items={items || []}
            qualityIndex={qualityIndex}
            isDragging={!!isDragging}
            isDraggingUp={!!isDraggingUp}
            isDraggingDown={!!isDraggingDown}
            connectDragSource={connectDragSource!}
            onDeleteGroupPress={onDeleteGroupPress!}
            onQualityProfileItemAllowedChange={
              onQualityProfileItemAllowedChange
            }
            onItemGroupAllowedChange={onItemGroupAllowedChange!}
            onItemGroupNameChange={onItemGroupNameChange!}
            onQualityProfileItemDragMove={onQualityProfileItemDragMove}
            onQualityProfileItemDragEnd={onQualityProfileItemDragEnd}
          />
        )}

        {qualityId != null && (
          <QualityProfileItem
            editGroups={editGroups}
            groupId={groupId}
            qualityId={qualityId}
            name={name}
            allowed={allowed}
            isDragging={!!isDragging}
            isOverCurrent={!!isOverCurrent}
            connectDragSource={connectDragSource!}
            onCreateGroupPress={onCreateGroupPress}
            onQualityProfileItemAllowedChange={
              onQualityProfileItemAllowedChange
            }
          />
        )}

        {isAfter && (
          <div
            className={classNames(
              styles.qualityProfileItemPlaceholder,
              styles.qualityProfileItemPlaceholderAfter
            )}
          />
        )}
      </div>
    );
  }
}

export default DropTarget(
  QUALITY_PROFILE_ITEM,
  qualityProfileItemDropTarget,
  collectDropTarget
)(
  DragSource(
    QUALITY_PROFILE_ITEM,
    qualityProfileItemDragSource,
    collectDragSource
  )(QualityProfileItemDragSourceComponent as unknown as React.ComponentType)
) as unknown as React.ComponentType<QualityProfileItemDragSourceProps>;
