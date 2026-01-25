import classNames from 'classnames';
import { useRef } from 'react';
import { DropTargetMonitor, useDrag, useDrop } from 'react-dnd';
import { QUALITY_PROFILE_ITEM } from 'Helpers/dragTypes';
import QualityProfileItem from './QualityProfileItem';
import QualityProfileItemGroup from './QualityProfileItemGroup';
import styles from './QualityProfileItemDragSource.css';

interface DragItem {
  qualityIndex: string;
  isGroup: boolean;
  editGroups: boolean;
  groupId?: number;
  qualityId?: number;
  name: string;
  allowed: boolean;
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
  isInGroup?: boolean;
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

function QualityProfileItemDragSource(
  props: QualityProfileItemDragSourceProps
) {
  const {
    editGroups,
    groupId,
    qualityId,
    name,
    allowed,
    items,
    qualityIndex,
    isDraggingUp,
    isDraggingDown,
    onCreateGroupPress,
    onDeleteGroupPress,
    onQualityProfileItemAllowedChange,
    onItemGroupAllowedChange,
    onItemGroupNameChange,
    onQualityProfileItemDragMove,
    onQualityProfileItemDragEnd,
  } = props;

  const ref = useRef<HTMLDivElement>(null);

  const [{ isDragging }, drag, dragPreview] = useDrag({
    type: QUALITY_PROFILE_ITEM,
    item: () => ({
      editGroups,
      qualityIndex,
      groupId,
      qualityId,
      isGroup: !qualityId,
      name,
      allowed,
    }),
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
    end: () => {
      onQualityProfileItemDragEnd(true);
    },
  });

  const [{ isOverCurrent }, drop] = useDrop({
    accept: QUALITY_PROFILE_ITEM,
    hover: (item: DragItem, monitor: DropTargetMonitor<DragItem>) => {
      if (!ref.current) {
        return;
      }

      const { qualityIndex: dragQualityIndex, isGroup: isDragGroup } = item;
      const dropQualityIndex = qualityIndex;
      const isDropGroupItem = !!(qualityId && groupId);

      const childNodeIndex = isOverCurrent && isDraggingUp ? 1 : 0;
      const hoverBoundingRect =
        ref.current.children[childNodeIndex].getBoundingClientRect();
      const hoverMiddleY =
        (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
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

      onQualityProfileItemDragMove({
        dragQualityIndex,
        dropQualityIndex,
        dropPosition,
      });
    },
    collect: (monitor) => ({
      isOver: monitor.isOver(),
      isOverCurrent: monitor.isOver({ shallow: true }),
    }),
  });

  dragPreview(drop(ref));

  const isBefore = !isDragging && isDraggingUp && isOverCurrent;
  const isAfter = !isDragging && isDraggingDown && isOverCurrent;

  return (
    <div
      ref={ref}
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
          isDragging={isDragging}
          isDraggingUp={!!isDraggingUp}
          isDraggingDown={!!isDraggingDown}
          dragRef={drag}
          onDeleteGroupPress={onDeleteGroupPress!}
          onQualityProfileItemAllowedChange={onQualityProfileItemAllowedChange}
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
          isDragging={isDragging}
          isOverCurrent={isOverCurrent}
          dragRef={drag}
          onCreateGroupPress={onCreateGroupPress}
          onQualityProfileItemAllowedChange={onQualityProfileItemAllowedChange}
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

export default QualityProfileItemDragSource;
