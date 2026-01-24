/* eslint-disable @typescript-eslint/no-explicit-any */
import classNames from 'classnames';
import React, { useCallback } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import TextInput from 'Components/Form/TextInput';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import QualityProfileItemDragSource from './QualityProfileItemDragSource';
import styles from './QualityProfileItemGroup.css';

interface GroupItem {
  quality: { id: number; name: string };
}

interface QualityProfileItemGroupProps {
  editGroups?: boolean;
  groupId: number;
  name: string;
  allowed: boolean;
  items: GroupItem[];
  qualityIndex: string;
  isDragging: boolean;
  isDraggingUp: boolean;
  isDraggingDown: boolean;
  connectDragSource?: (node: React.ReactElement) => React.ReactElement;
  onItemGroupAllowedChange: (groupId: number, value: boolean) => void;
  onQualityProfileItemAllowedChange: (
    qualityId: number,
    value: boolean
  ) => void;
  onItemGroupNameChange: (groupId: number, value: string) => void;
  onDeleteGroupPress: (groupId: number, value?: boolean) => void;
  onQualityProfileItemDragMove: (payload: any) => void;
  onQualityProfileItemDragEnd: (didDrop: boolean) => void;
}

function QualityProfileItemGroup({
  editGroups,
  groupId,
  name,
  allowed,
  items,
  qualityIndex,
  isDragging,
  isDraggingUp,
  isDraggingDown,
  connectDragSource = (node) => node,
  onItemGroupAllowedChange,
  onQualityProfileItemAllowedChange,
  onItemGroupNameChange,
  onDeleteGroupPress,
  onQualityProfileItemDragMove,
  onQualityProfileItemDragEnd,
}: QualityProfileItemGroupProps) {
  const handleAllowedChange = useCallback(
    ({ value }: { value: boolean }) => {
      onItemGroupAllowedChange(groupId, value);
    },
    [groupId, onItemGroupAllowedChange]
  );

  const handleNameChange = useCallback(
    ({ value }: { value: string }) => {
      onItemGroupNameChange(groupId, value);
    },
    [groupId, onItemGroupNameChange]
  );

  const handleDeleteGroupPress = useCallback(() => {
    onDeleteGroupPress(groupId);
  }, [groupId, onDeleteGroupPress]);

  return (
    <div
      className={classNames(
        styles.qualityProfileItemGroup,
        editGroups && styles.editGroups,
        isDragging && styles.isDragging
      )}
    >
      <div className={styles.qualityProfileItemGroupInfo}>
        {editGroups && (
          <div className={styles.qualityNameContainer}>
            <IconButton
              className={styles.deleteGroupButton}
              name={icons.UNGROUP}
              title={translate('Ungroup')}
              onPress={handleDeleteGroupPress}
            />

            <TextInput
              className={styles.nameInput}
              name="name"
              value={name}
              onChange={handleNameChange}
            />
          </div>
        )}

        {!editGroups && (
          <label className={styles.qualityNameLabel}>
            <CheckInput
              className={styles.checkInput}
              containerClassName={styles.checkInputContainer}
              name="allowed"
              value={allowed}
              onChange={handleAllowedChange}
            />

            <div className={styles.nameContainer}>
              <div
                className={classNames(
                  styles.name,
                  !allowed && styles.notAllowed
                )}
              >
                {name}
              </div>

              <div className={styles.groupQualities}>
                {items
                  .map(({ quality }) => {
                    return <Label key={quality.id}>{quality.name}</Label>;
                  })
                  .reverse()}
              </div>
            </div>
          </label>
        )}

        {connectDragSource(
          <div className={styles.dragHandle}>
            <Icon
              className={styles.dragIcon}
              name={icons.REORDER}
              title={translate('Reorder')}
            />
          </div>
        )}
      </div>

      {editGroups && (
        <div className={styles.items}>
          {items
            .map(({ quality }, index) => {
              return (
                <QualityProfileItemDragSource
                  key={quality.id}
                  editGroups={editGroups}
                  groupId={groupId}
                  qualityId={quality.id}
                  name={quality.name}
                  allowed={allowed}
                  items={items}
                  qualityIndex={`${qualityIndex}.${index + 1}`}
                  isDragging={isDragging}
                  isDraggingUp={isDraggingUp}
                  isDraggingDown={isDraggingDown}
                  isInGroup={true}
                  onQualityProfileItemAllowedChange={
                    onQualityProfileItemAllowedChange
                  }
                  onQualityProfileItemDragMove={onQualityProfileItemDragMove}
                  onQualityProfileItemDragEnd={onQualityProfileItemDragEnd}
                />
              );
            })
            .reverse()}
        </div>
      )}
    </div>
  );
}

export default QualityProfileItemGroup;
