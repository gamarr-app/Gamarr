import classNames from 'classnames';
import React, { useCallback } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './QualityProfileItem.css';

interface QualityProfileItemProps {
  editGroups?: boolean;
  isPreview?: boolean;
  groupId?: number;
  qualityId: number;
  name: string;
  allowed: boolean;
  isDragging: boolean;
  isOverCurrent?: boolean;
  isInGroup?: boolean;
  connectDragSource?: (node: React.ReactElement) => React.ReactElement | null;
  onCreateGroupPress?: (qualityId: number) => void;
  onQualityProfileItemAllowedChange?: (
    qualityId: number,
    value: boolean
  ) => void;
}

function QualityProfileItem({
  editGroups,
  isPreview = false,
  groupId,
  qualityId,
  name,
  allowed,
  isDragging,
  isOverCurrent = false,
  connectDragSource = (node) => node,
  onCreateGroupPress,
  onQualityProfileItemAllowedChange,
}: QualityProfileItemProps) {
  const handleAllowedChange = useCallback(
    ({ value }: { value: boolean }) => {
      if (onQualityProfileItemAllowedChange) {
        onQualityProfileItemAllowedChange(qualityId, value);
      }
    },
    [qualityId, onQualityProfileItemAllowedChange]
  );

  const handleCreateGroupPress = useCallback(() => {
    if (onCreateGroupPress) {
      onCreateGroupPress(qualityId);
    }
  }, [qualityId, onCreateGroupPress]);

  return (
    <div
      className={classNames(
        styles.qualityProfileItem,
        isDragging && styles.isDragging,
        isPreview && styles.isPreview,
        isOverCurrent && styles.isOverCurrent,
        groupId && styles.isInGroup
      )}
    >
      <label className={styles.qualityNameContainer}>
        {editGroups && !groupId && !isPreview && (
          <IconButton
            className={styles.createGroupButton}
            name={icons.GROUP}
            title={translate('Group')}
            onPress={handleCreateGroupPress}
          />
        )}

        {!editGroups && (
          <CheckInput
            className={styles.checkInput}
            containerClassName={styles.checkInputContainer}
            name={name}
            value={allowed}
            isDisabled={!!groupId}
            onChange={handleAllowedChange}
          />
        )}

        <div
          className={classNames(
            styles.qualityName,
            groupId && styles.isInGroup,
            !allowed && styles.notAllowed
          )}
        >
          {name}
        </div>
      </label>

      {connectDragSource(
        <div className={styles.dragHandle}>
          <Icon
            className={styles.dragIcon}
            title={translate('CreateGroup')}
            name={icons.REORDER}
          />
        </div>
      )}
    </div>
  );
}

export default QualityProfileItem;
