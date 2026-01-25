import classNames from 'classnames';
import React, { useCallback, useState } from 'react';
import { Tag } from 'App/State/TagsAppState';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { icons, kinds } from 'Helpers/Props';
import titleCase from 'Utilities/String/titleCase';
import translate from 'Utilities/String/translate';
import EditDelayProfileModal from './EditDelayProfileModal';
import styles from './DelayProfile.css';

function getDelay(enabled: boolean, delay: number) {
  if (!enabled) {
    return '-';
  }

  if (!delay) {
    return translate('NoDelay');
  }

  if (delay === 1) {
    return translate('OneMinute');
  }

  return translate('DelayMinutes', { delay });
}

interface DelayProfileProps {
  id: number;
  enableUsenet: boolean;
  enableTorrent: boolean;
  preferredProtocol: string;
  usenetDelay: number;
  torrentDelay: number;
  tags: number[];
  tagList: Tag[];
  isDragging: boolean;
  connectDragSource?: (node: React.ReactElement) => React.ReactElement | null;
  onConfirmDeleteDelayProfile: (id: number) => void;
}

function DelayProfile({
  id,
  enableUsenet,
  enableTorrent,
  preferredProtocol,
  usenetDelay,
  torrentDelay,
  tags,
  tagList,
  isDragging,
  connectDragSource = (node) => node,
  onConfirmDeleteDelayProfile,
}: DelayProfileProps) {
  const [isEditDelayProfileModalOpen, setIsEditDelayProfileModalOpen] =
    useState(false);
  const [isDeleteDelayProfileModalOpen, setIsDeleteDelayProfileModalOpen] =
    useState(false);

  const handleEditDelayProfilePress = useCallback(() => {
    setIsEditDelayProfileModalOpen(true);
  }, []);

  const handleEditDelayProfileModalClose = useCallback(() => {
    setIsEditDelayProfileModalOpen(false);
  }, []);

  const handleDeleteDelayProfilePress = useCallback(() => {
    setIsEditDelayProfileModalOpen(false);
    setIsDeleteDelayProfileModalOpen(true);
  }, []);

  const handleDeleteDelayProfileModalClose = useCallback(() => {
    setIsDeleteDelayProfileModalOpen(false);
  }, []);

  const handleConfirmDeleteDelayProfile = useCallback(() => {
    onConfirmDeleteDelayProfile(id);
  }, [id, onConfirmDeleteDelayProfile]);

  let preferred = titleCase(translate('PreferProtocol', { preferredProtocol }));

  if (!enableUsenet) {
    preferred = translate('OnlyTorrent');
  } else if (!enableTorrent) {
    preferred = translate('OnlyUsenet');
  }

  return (
    <div
      className={classNames(
        styles.delayProfile,
        isDragging && styles.isDragging
      )}
    >
      <div className={styles.column}>{preferred}</div>
      <div className={styles.column}>{getDelay(enableUsenet, usenetDelay)}</div>
      <div className={styles.column}>
        {getDelay(enableTorrent, torrentDelay)}
      </div>

      <TagList tags={tags} tagList={tagList} />

      <div className={styles.actions}>
        <Link
          className={id === 1 ? styles.editButton : undefined}
          onPress={handleEditDelayProfilePress}
        >
          <Icon name={icons.EDIT} />
        </Link>

        {id !== 1 &&
          connectDragSource(
            <div className={styles.dragHandle}>
              <Icon className={styles.dragIcon} name={icons.REORDER} />
            </div>
          )}
      </div>

      <EditDelayProfileModal
        id={id}
        isOpen={isEditDelayProfileModalOpen}
        onModalClose={handleEditDelayProfileModalClose}
        onDeleteDelayProfilePress={handleDeleteDelayProfilePress}
      />

      <ConfirmModal
        isOpen={isDeleteDelayProfileModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteDelayProfile')}
        message={translate('DeleteDelayProfileMessageText')}
        confirmLabel={translate('Delete')}
        onConfirm={handleConfirmDeleteDelayProfile}
        onCancel={handleDeleteDelayProfileModalClose}
      />
    </div>
  );
}

export default DelayProfile;
